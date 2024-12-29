using System.Collections.Generic;
using System;
using System.Linq.Expressions;
using Connected.Data.Expressions.Translation.Resolvers;
using Connected.Data.Expressions.Expressions;
using Connected.Data.Expressions.Languages;
using Connected.Data.Expressions.Visitors;

namespace Connected.Data.Expressions.Translation.Rewriters;

public sealed class OrderByRewriter : DatabaseVisitor
{
	private OrderByRewriter(QueryLanguage language)
	{
		Language = language;
		IsOuterMostSelect = true;
	}

	private QueryLanguage Language { get; }
	private IList<OrderExpression>? ResolvedOrderings { get; set; }
	private bool IsOuterMostSelect { get; set; }

	public static Expression Rewrite(QueryLanguage language, Expression expression)
	{
		if (new OrderByRewriter(language).Visit(expression) is not Expression orderByExpression)
			throw new NullReferenceException(nameof(orderByExpression));

		return orderByExpression;
	}

	protected override Expression VisitSelect(SelectExpression select)
	{
		bool saveIsOuterMostSelect = IsOuterMostSelect;

		try
		{
			IsOuterMostSelect = false;
			select = (SelectExpression)base.VisitSelect(select);

			var hasOrderBy = select.OrderBy is not null && select.OrderBy.Count > 0;
			var hasGroupBy = select.GroupBy is not null && select.GroupBy.Count > 0;
			var canHaveOrderBy = saveIsOuterMostSelect || select.Take is not null || select.Skip is not null;
			var canReceiveOrderings = canHaveOrderBy && !hasGroupBy && !select.IsDistinct && !AggregateChecker.HasAggregates(select);

			if (hasOrderBy)
				PrependOrderings(select.OrderBy);

			if (select.IsReverse)
				ReverseOrderings();

			IEnumerable<OrderExpression>? orderings = null;

			if (canReceiveOrderings)
				orderings = ResolvedOrderings;

			else if (canHaveOrderBy)
				orderings = select.OrderBy;

			var canPassOnOrderings = !saveIsOuterMostSelect && !hasGroupBy && !select.IsDistinct;
			var columns = select.Columns;

			if (ResolvedOrderings is not null)
			{
				if (canPassOnOrderings)
				{
					var producedAliases = DeclaredAliasesResolver.Resolve(select.From);
					var project = RebindOrderings(ResolvedOrderings, select.Alias, producedAliases, select.Columns);

					ResolvedOrderings = null;

					PrependOrderings(project.Orderings);

					columns = project.Columns;
				}
				else
					ResolvedOrderings = null;
			}
			if (orderings != select.OrderBy || columns != select.Columns || select.IsReverse)
				select = new SelectExpression(select.Alias, columns, select.From, select.Where, orderings, select.GroupBy, select.IsDistinct, select.Skip, select.Take, false);

			return select;
		}
		finally
		{
			IsOuterMostSelect = saveIsOuterMostSelect;
		}
	}

	protected override Expression VisitSubquery(SubqueryExpression subquery)
	{
		var saveOrderings = ResolvedOrderings;

		ResolvedOrderings = null;

		var result = base.VisitSubquery(subquery);

		ResolvedOrderings = saveOrderings;

		return result;
	}

	protected override Expression VisitJoin(JoinExpression join)
	{
		var left = VisitSource(join.Left);
		var leftOrders = ResolvedOrderings;
		/*
	  * start on the right with a clean slate
	  */
		ResolvedOrderings = null;

		var right = VisitSource(join.Right);

		PrependOrderings(leftOrders);

		var condition = Visit(join.Condition);

		if (left != join.Left || right != join.Right || condition != join.Condition)
			return new JoinExpression(join.Join, left, right, condition);

		return join;
	}

	private void PrependOrderings(IList<OrderExpression>? newOrderings)
	{
		if (newOrderings is not null)
		{
			ResolvedOrderings ??= new List<OrderExpression>();

			for (var i = newOrderings.Count - 1; i >= 0; i--)
				ResolvedOrderings.Insert(0, newOrderings[i]);

			var unique = new HashSet<string>();

			for (var i = 0; i < ResolvedOrderings.Count;)
			{
				if (ResolvedOrderings[i].Expression is ColumnExpression column)
				{
					var hash = $"{column.Alias}:{column.Name}";

					if (unique.Contains(hash))
					{
						ResolvedOrderings.RemoveAt(i);

						continue;
					}
					else
						unique.Add(hash);
				}

				i++;
			}
		}
	}

	private void ReverseOrderings()
	{
		if (ResolvedOrderings is not null)
		{
			for (var i = 0; i < ResolvedOrderings.Count; i++)
			{
				var ord = ResolvedOrderings[i];

				ResolvedOrderings[i] = new OrderExpression(ord.OrderType == OrderType.Ascending ? OrderType.Descending : OrderType.Ascending, ord.Expression);
			}
		}
	}

	private BindResultRewriter RebindOrderings(IEnumerable<OrderExpression> orderings, Alias alias, HashSet<Alias> existingAliases, IEnumerable<ColumnDeclaration> existingColumns)
	{
		List<ColumnDeclaration>? newColumns = null;
		List<OrderExpression> newOrderings = new();

		foreach (var ordering in orderings)
		{
			var expr = ordering.Expression;
			var column = expr as ColumnExpression;

			if (column is null || existingAliases is not null && existingAliases.Contains(column.Alias))
			{
				var ordinal = 0;

				foreach (var existingColumn in existingColumns)
				{
					var declColumn = existingColumn.Expression as ColumnExpression;

					if (existingColumn.Expression == ordering.Expression || column is not null && declColumn is not null && column.Alias == declColumn.Alias && column.Name == declColumn.Name)
					{
						expr = new ColumnExpression(column.Type, column.QueryType, alias, existingColumn.Name);

						break;
					}

					ordinal++;
				}
				if (expr == ordering.Expression)
				{
					if (newColumns is null)
					{
						newColumns = new List<ColumnDeclaration>(existingColumns);
						existingColumns = newColumns;
					}

					var colName = column != null ? column.Name : $"c{ordinal}";

					colName = newColumns.ResolveAvailableColumnName(colName);

					var colType = Language.TypeSystem.ResolveColumnType(expr.Type);

					newColumns.Add(new ColumnDeclaration(colName, ordering.Expression, colType));

					expr = new ColumnExpression(expr.Type, colType, alias, colName);
				}

				newOrderings.Add(new OrderExpression(ordering.OrderType, expr));
			}
		}

		return new BindResultRewriter(existingColumns, newOrderings);
	}
}
