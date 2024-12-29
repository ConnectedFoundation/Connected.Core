using System.Linq.Expressions;
using System;
using Connected.Data.Expressions.Translation;
using Connected.Data.Expressions.Expressions;
using Connected.Data.Expressions.Visitors;

namespace Connected.Data.Expressions.Translation.Optimization;

internal sealed class SubqueryMerger : DatabaseVisitor
{
	private SubqueryMerger()
	{
	}

	internal static Expression Merge(Expression expression)
	{
		if (new SubqueryMerger().Visit(expression) is not Expression subqueryExpression)
			throw new NullReferenceException(nameof(subqueryExpression));

		return subqueryExpression;
	}

	private bool IsTopLevel { get; set; } = true;

	protected override Expression VisitSelect(SelectExpression expression)
	{
		var wasTopLevel = IsTopLevel;

		IsTopLevel = false;

		expression = (SelectExpression)base.VisitSelect(expression);

		while (CanMergeWithFrom(expression, wasTopLevel))
		{
			if (GetLeftMostSelect(expression.From) is not SelectExpression fromSelectExpression)
				throw new NullReferenceException(nameof(fromSelectExpression));

			expression = Subqueries.Remove(expression, fromSelectExpression);

			var where = expression.Where;

			if (fromSelectExpression.Where is not null)
			{
				if (where is not null)
					where = fromSelectExpression.Where.And(where);
				else
					where = fromSelectExpression.Where;
			}

			var orderBy = expression.OrderBy is not null && expression.OrderBy.Count > 0 ? expression.OrderBy : fromSelectExpression.OrderBy;
			var groupBy = expression.GroupBy is not null && expression.GroupBy.Count > 0 ? expression.GroupBy : fromSelectExpression.GroupBy;
			var skip = expression.Skip is not null ? expression.Skip : fromSelectExpression.Skip;
			var take = expression.Take is not null ? expression.Take : fromSelectExpression.Take;
			var isDistinct = expression.IsDistinct | fromSelectExpression.IsDistinct;

			if (where != expression.Where || orderBy != expression.OrderBy || groupBy != expression.GroupBy || isDistinct != expression.IsDistinct || skip != expression.Skip || take != expression.Take)
				expression = new SelectExpression(expression.Alias, expression.Columns, expression.From, where, orderBy, groupBy, isDistinct, skip, take, expression.IsReverse);
		}

		return expression;
	}

	private static SelectExpression? GetLeftMostSelect(Expression source)
	{
		var select = source as SelectExpression;

		if (select is not null)
			return select;

		if (source is JoinExpression join)
			return GetLeftMostSelect(join.Left);

		return null;
	}

	private static bool IsColumnProjection(SelectExpression select)
	{
		for (var i = 0; i < select.Columns.Count; i++)
		{
			var cd = select.Columns[i];

			if (cd.Expression.NodeType != (ExpressionType)DatabaseExpressionType.Column && cd.Expression.NodeType != ExpressionType.Constant)
				return false;
		}

		return true;
	}

	private static bool CanMergeWithFrom(SelectExpression select, bool isTopLevel)
	{
		var fromSelect = GetLeftMostSelect(select.From);

		if (fromSelect is null)
			return false;

		if (!IsColumnProjection(fromSelect))
			return false;

		var selHasNameMapProjection = RedundantSubqueries.IsNameMapProjection(select);
		var selHasOrderBy = select.OrderBy is not null && select.OrderBy.Count > 0;
		var selHasGroupBy = select.GroupBy is not null && select.GroupBy.Count > 0;
		var selHasAggregates = AggregateChecker.HasAggregates(select);
		var selHasJoin = select.From is JoinExpression;
		var frmHasOrderBy = fromSelect.OrderBy is not null && fromSelect.OrderBy.Count > 0;
		var frmHasGroupBy = fromSelect.GroupBy is not null && fromSelect.GroupBy.Count > 0;
		var frmHasAggregates = AggregateChecker.HasAggregates(fromSelect);

		if (selHasOrderBy && frmHasOrderBy)
			return false;

		if (selHasGroupBy && frmHasGroupBy)
			return false;

		if (select.IsReverse || fromSelect.IsReverse)
			return false;

		if (frmHasOrderBy && (selHasGroupBy || selHasAggregates || select.IsDistinct))
			return false;

		if (frmHasGroupBy)
			return false;

		if (fromSelect.Take is not null && (select.Take is not null || select.Skip is not null || select.IsDistinct || selHasAggregates || selHasGroupBy || selHasJoin))
			return false;

		if (fromSelect.Skip is not null && (select.Skip is not null || select.IsDistinct || selHasAggregates || selHasGroupBy || selHasJoin))
			return false;

		if (fromSelect.IsDistinct && (select.Take is not null || select.Skip is not null || !selHasNameMapProjection || selHasGroupBy || selHasAggregates || selHasOrderBy && !isTopLevel || selHasJoin))
			return false;

		if (frmHasAggregates && (select.Take is not null || select.Skip is not null || select.IsDistinct || selHasAggregates || selHasGroupBy || selHasJoin))
			return false;

		return true;
	}
}