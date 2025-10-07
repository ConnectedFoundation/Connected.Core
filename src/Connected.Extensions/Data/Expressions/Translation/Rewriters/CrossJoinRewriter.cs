using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using Connected.Data.Expressions.Translation.Resolvers;
using Connected.Data.Expressions.Expressions;
using Connected.Data.Expressions.Visitors;

namespace Connected.Data.Expressions.Translation.Rewriters;

public sealed class CrossJoinRewriter : DatabaseVisitor
{
	public static Expression Rewrite(Expression expression)
	{
		if (new CrossJoinRewriter().Visit(expression) is not Expression crossJoinExpression)
			throw new NullReferenceException(nameof(crossJoinExpression));

		return crossJoinExpression;
	}

	private Expression? CurrentWhere { get; set; }

	protected override Expression VisitSelect(SelectExpression select)
	{
		var saveWhere = CurrentWhere;

		try
		{
			CurrentWhere = select.Where;

			var result = (SelectExpression)base.VisitSelect(select);

			if (CurrentWhere != result.Where)
				return result.SetWhere(CurrentWhere);

			return result;
		}
		finally
		{
			CurrentWhere = saveWhere;
		}
	}

	protected override Expression VisitJoin(JoinExpression expression)
	{
		expression = (JoinExpression)base.VisitJoin(expression);

		if (expression.Join == JoinType.CrossJoin && CurrentWhere is not null)
		{
			var declaredLeft = DeclaredAliasesResolver.Resolve(expression.Left);
			var declaredRight = DeclaredAliasesResolver.Resolve(expression.Right);
			var declared = new HashSet<Alias>(declaredLeft.Union(declaredRight));
			var exprs = CurrentWhere.Split(ExpressionType.And, ExpressionType.AndAlso);
			var good = exprs.Where(e => CanBeJoinCondition(e, declaredLeft, declaredRight, declared)).ToList();

			if (good.Any())
			{
				if (good.Join(ExpressionType.And) is not Expression conditionExpression)
					throw new NullReferenceException(nameof(conditionExpression));

				expression = UpdateJoin(expression, JoinType.InnerJoin, expression.Left, expression.Right, conditionExpression);

				var newWhere = exprs.Where(e => !good.Contains(e)).Join(ExpressionType.And);

				CurrentWhere = newWhere;
			}
		}

		return expression;
	}

	private static bool CanBeJoinCondition(Expression expression, HashSet<Alias> left, HashSet<Alias> right, HashSet<Alias> all)
	{
		var referenced = ReferencedAliasesResolver.Resolve(expression);
		var leftOkay = referenced.Intersect(left).Any();
		var rightOkay = referenced.Intersect(right).Any();
		var subset = referenced.IsSubsetOf(all);

		return leftOkay && rightOkay && subset;
	}
}