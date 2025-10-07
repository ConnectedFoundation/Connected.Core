using System.Linq.Expressions;
using System;
using System.Collections.Generic;
using Connected.Data.Expressions.Expressions;
using Connected.Data.Expressions.Translation.Projections;
using Connected.Data.Expressions.Visitors;

namespace Connected.Data.Expressions.Translation;

internal sealed class RelationshipBinder : DatabaseVisitor
{
	private RelationshipBinder(ExpressionCompilationContext context)
	{
		Context = context;
	}

	private ExpressionCompilationContext Context { get; }
	private Expression? CurrentFrom { get; set; }

	public static Expression Bind(ExpressionCompilationContext context, Expression expression)
	{
		if (new RelationshipBinder(context).Visit(expression) is not Expression relationshipExpression)
			throw new NullReferenceException(nameof(relationshipExpression));

		return relationshipExpression;
	}

	protected override Expression VisitSelect(SelectExpression select)
	{
		/*
		 * look for association references in SelectExpression clauses
		 */
		var saveCurrentFrom = CurrentFrom;

		CurrentFrom = VisitSource(select.From);

		try
		{
			var where = Visit(select.Where);
			var orderBy = VisitOrderBy(select.OrderBy);
			var groupBy = VisitExpressionList(select.GroupBy);
			var skip = Visit(select.Skip);
			var take = Visit(select.Take);
			var columns = VisitColumnDeclarations(select.Columns);

			return UpdateSelect(select, CurrentFrom, where, orderBy, groupBy, skip, take, select.IsDistinct, select.IsReverse, columns);
		}
		finally
		{
			CurrentFrom = saveCurrentFrom;
		}
	}

	protected override Expression VisitProjection(ProjectionExpression proj)
	{
		var select = (SelectExpression)Visit(proj.Select);
		var saveCurrentFrom = CurrentFrom;

		CurrentFrom = select;

		try
		{
			var projector = Visit(proj.Projector);

			if (CurrentFrom != select)
			{
				var alias = Alias.New();
				var existingAliases = GetAliases(CurrentFrom);
				var pc = ColumnProjector.ProjectColumns(Context.Language, projector, null, alias, existingAliases);

				projector = pc.Projector;
				select = new SelectExpression(alias, pc.Columns, CurrentFrom, null);
			}

			return UpdateProjection(proj, select, projector, proj.Aggregator);
		}
		finally
		{
			CurrentFrom = saveCurrentFrom;
		}
	}

	private static List<Alias> GetAliases(Expression expression)
	{
		var aliases = new List<Alias>();

		GetAliases(expression);

		return aliases;

		void GetAliases(Expression e)
		{
			switch (e)
			{
				case JoinExpression j:
					GetAliases(j.Left);
					GetAliases(j.Right);
					break;
				case AliasedExpression a:
					aliases.Add(a.Alias);
					break;
			}
		}
	}

	protected override Expression VisitMemberAccess(MemberExpression expression)
	{
		var source = Visit(expression.Expression);
		var result = Binder.Bind(source, expression.Member);
		var mex = result as MemberExpression;

		if (mex is not null && mex.Member == expression.Member && mex.Expression == expression.Expression)
			return expression;

		return result;
	}
}
