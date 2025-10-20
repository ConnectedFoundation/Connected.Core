using Connected.Data.Expressions.Expressions;
using Connected.Data.Expressions.Translation.Resolvers;
using Connected.Data.Expressions.Visitors;
using System.Linq.Expressions;

namespace Connected.Data.Expressions.Translation.Optimization;

internal sealed class RedundantSubqueries
	: DatabaseVisitor
{
	private RedundantSubqueries()
	{
	}

	public static Expression Remove(Expression expression)
	{
		if (new RedundantSubqueries().Visit(expression) is not Expression redundandSubqueryExpression)
			throw new NullReferenceException(nameof(redundandSubqueryExpression));

		return SubqueryMerger.Merge(redundandSubqueryExpression);
	}

	protected override Expression VisitSelect(SelectExpression select)
	{
		select = (SelectExpression)base.VisitSelect(select);

		var redundant = RedundandSubqueriesResolver.Resolve(select.From);

		if (redundant is not null)
			select = Subqueries.Remove(select, redundant);

		return select;
	}

	protected override Expression VisitProjection(ProjectionExpression proj)
	{
		proj = (ProjectionExpression)base.VisitProjection(proj);

		if (proj.Select.From is SelectExpression)
		{
			var redundant = RedundandSubqueriesResolver.Resolve(proj.Select);

			if (redundant is not null)
				proj = Subqueries.Remove(proj, redundant);
		}

		return proj;
	}

	internal static bool IsNameMapProjection(SelectExpression select)
	{
		if (select.From is TableExpression)
			return false;


		if (select.From is not SelectExpression fromSelect || select.Columns.Count != fromSelect.Columns.Count)
			return false;

		var fromColumns = fromSelect.Columns;

		for (var i = 0; i < select.Columns.Count; i++)
		{
			if (select.Columns[i].Expression is not ColumnExpression col || !(col.Name == fromColumns[i].Name))
				return false;
		}

		return true;
	}

	internal static bool IsInitialProjection(SelectExpression select)
	{
		return select.From is TableExpression;
	}
}