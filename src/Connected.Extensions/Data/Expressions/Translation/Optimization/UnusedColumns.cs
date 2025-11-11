using Connected.Data.Expressions.Expressions;
using Connected.Data.Expressions.Visitors;
using System.Linq.Expressions;

namespace Connected.Data.Expressions.Translation.Optimization;

internal sealed class UnusedColumns
	: DatabaseVisitor
{
	private UnusedColumns()
	{
		AllUsed = [];
	}
	private Dictionary<Alias, HashSet<string>> AllUsed { get; set; }

	private bool RetainAllColumns { get; set; }

	public static Expression Remove(Expression expression)
	{
		if (new UnusedColumns().Visit(expression) is not Expression unusedColumnExpression)
			throw new NullReferenceException(nameof(unusedColumnExpression));

		return unusedColumnExpression;
	}

	private void MarkAsUsed(Alias alias, string name)
	{
		if (!AllUsed.TryGetValue(alias, out HashSet<string>? columns))
		{
			columns = [];

			AllUsed.Add(alias, columns);
		}

		columns.Add(name);
	}

	private bool IsUsed(Alias alias, string name)
	{
		if (AllUsed.TryGetValue(alias, out HashSet<string>? columnsUsed))
		{
			if (columnsUsed is not null)
				return columnsUsed.Contains(name);
		}

		return false;
	}

	private void ClearUsed(Alias alias)
	{
		AllUsed[alias] = [];
	}

	protected override Expression VisitColumn(ColumnExpression expression)
	{
		MarkAsUsed(expression.Alias, expression.Name);

		return expression;
	}

	protected override Expression VisitSubquery(SubqueryExpression subquery)
	{
		if ((subquery.NodeType == (ExpressionType)DatabaseExpressionType.Scalar || subquery.NodeType == (ExpressionType)DatabaseExpressionType.In) && subquery.Select is not null)
		{
			System.Diagnostics.Debug.Assert(subquery.Select.Columns.Count == 1);
			MarkAsUsed(subquery.Select.Alias, subquery.Select.Columns[0].Name);
		}

		return base.VisitSubquery(subquery);
	}

	protected override Expression VisitSelect(SelectExpression select)
	{
		var columns = select.Columns;
		var wasRetained = RetainAllColumns;

		RetainAllColumns = false;

		List<ColumnDeclaration>? alternate = null;

		for (var i = 0; i < select.Columns.Count; i++)
		{
			var decl = select.Columns[i];

			if (wasRetained || select.IsDistinct || IsUsed(select.Alias, decl.Name))
			{
				if (Visit(decl.Expression) is not Expression declarationExpression)
					throw new NullReferenceException(nameof(declarationExpression));

				if (declarationExpression != decl.Expression)
					decl = new ColumnDeclaration(decl.Name, declarationExpression, decl.DataType);
			}
			else
				decl = null;

			if (decl != select.Columns[i] && alternate is null)
			{
				alternate = [];

				for (var j = 0; j < i; j++)
					alternate.Add(select.Columns[j]);
			}

			if (decl is not null && alternate is not null)
				alternate.Add(decl);
		}

		if (alternate is not null)
			columns = alternate.AsReadOnly();

		var take = select.Take is null ? null : Visit(select.Take);
		var skip = select.Skip is null ? null : Visit(select.Skip);
		var groupbys = VisitExpressionList(select.GroupBy);
		var orderbys = VisitOrderBy(select.OrderBy);
		var where = select.Where is null ? null : Visit(select.Where);

		if (Visit(select.From) is not Expression fromExpression)
			throw new NullReferenceException(nameof(fromExpression));

		ClearUsed(select.Alias);

		if (columns != select.Columns || take != select.Take || skip != select.Skip || orderbys != select.OrderBy || groupbys != select.GroupBy || where != select.Where || fromExpression != select.From)
			select = new SelectExpression(select.Alias, columns, fromExpression, where, orderbys, groupbys, select.IsDistinct, skip, take, select.IsReverse);

		RetainAllColumns = wasRetained;

		return select;
	}

	protected override Expression VisitAggregate(AggregateExpression expression)
	{
		/*
     * COUNT(*) forces all columns to be retained in subquery
     */
		if (string.Equals(expression.AggregateName, "Count", StringComparison.OrdinalIgnoreCase) && expression.Argument is null)
			RetainAllColumns = true;

		return base.VisitAggregate(expression);
	}

	protected override Expression VisitProjection(ProjectionExpression expression)
	{
		if (Visit(expression.Projector) is not Expression projector)
			throw new NullReferenceException(nameof(projector));

		if (Visit(expression.Select) is not SelectExpression selectExpression)
			throw new NullReferenceException(nameof(selectExpression));

		return UpdateProjection(expression, selectExpression, projector, expression.Aggregator);
	}

	protected override Expression VisitClientJoin(ClientJoinExpression expression)
	{
		var innerKey = VisitExpressionList(expression.InnerKey);
		var outerKey = VisitExpressionList(expression.OuterKey);

		if (Visit(expression.Projection) is not ProjectionExpression projectionExpression)
			throw new NullReferenceException(nameof(projectionExpression));

		if (projectionExpression != expression.Projection || innerKey != expression.InnerKey || outerKey != expression.OuterKey)
			return new ClientJoinExpression(projectionExpression, outerKey, innerKey);

		return expression;
	}

	protected override Expression VisitJoin(JoinExpression expression)
	{
		if (expression.Join == JoinType.SingletonLeftOuter)
		{
			var right = Visit(expression.Right);
			var ax = right as AliasedExpression;

			if (ax is not null && !AllUsed.ContainsKey(ax.Alias))
			{
				if (Visit(expression.Left) is not Expression leftOuterExpression)
					throw new NullReferenceException(nameof(leftOuterExpression));

				return leftOuterExpression;
			}

			if (expression.Condition is null || Visit(expression.Condition) is not Expression conditionExpression)
				throw new NullReferenceException(nameof(conditionExpression));

			if (expression.Left is null || Visit(expression.Left) is not Expression leftExpression)
				throw new NullReferenceException(nameof(leftExpression));

			if (expression.Right is null || Visit(expression.Right) is not Expression rightExpression)
				throw new NullReferenceException(nameof(rightExpression));

			return UpdateJoin(expression, expression.Join, leftExpression, rightExpression, conditionExpression);
		}
		else
		{
			if (expression.Condition is null || Visit(expression.Condition) is not Expression conditionExpression)
				throw new NullReferenceException(nameof(conditionExpression));

			var right = VisitSource(expression.Right);
			var left = VisitSource(expression.Left);

			return UpdateJoin(expression, expression.Join, left, right, conditionExpression);
		}
	}
}