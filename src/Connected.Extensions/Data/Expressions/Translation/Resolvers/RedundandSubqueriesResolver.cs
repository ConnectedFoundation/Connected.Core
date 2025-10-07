using System.Linq.Expressions;
using System.Collections.Generic;
using System.Linq;
using Connected.Data.Expressions.Expressions;
using Connected.Data.Expressions.Visitors;
using Connected.Data.Expressions.Translation.Optimization;

namespace Connected.Data.Expressions.Translation.Resolvers;

internal class RedundandSubqueriesResolver : DatabaseVisitor
{
	private RedundandSubqueriesResolver()
	{
	}

	private List<SelectExpression>? Redundant { get; set; }

	internal static List<SelectExpression> Resolve(Expression expression)
	{
		var retriever = new RedundandSubqueriesResolver();

		retriever.Visit(expression);

		return retriever.Redundant;
	}

	private static bool IsRedudantSubquery(SelectExpression expression)
	{
		return (IsSimpleProjection(expression) || RedundantSubqueries.IsNameMapProjection(expression))
			 && !expression.IsDistinct
			 && !expression.IsReverse
			 && expression.Take is null
			 && expression.Skip is null
			 && expression.Where is null
			 && (expression.OrderBy is null || !expression.OrderBy.Any())
			 && (expression.GroupBy is null || !expression.GroupBy.Any());
	}

	internal static bool IsSimpleProjection(SelectExpression select)
	{
		foreach (var decl in select.Columns)
		{
			if (decl.Expression is not ColumnExpression col || decl.Name != col.Name)
				return false;
		}

		return true;
	}

	protected override Expression VisitSelect(SelectExpression expression)
	{
		if (IsRedudantSubquery(expression))
		{
			Redundant ??= new List<SelectExpression>();

			Redundant.Add(expression);
		}

		return expression;
	}

	protected override Expression VisitSubquery(SubqueryExpression expression)
	{
		return expression;
	}
}