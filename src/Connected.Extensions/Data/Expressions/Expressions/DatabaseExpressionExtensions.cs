using Connected.Data.Expressions.Languages;
using Connected.Data.Expressions.Translation;
using Connected.Data.Expressions.Translation.Optimization;
using System.Linq.Expressions;

namespace Connected.Data.Expressions.Expressions;

internal static class DatabaseExpressionExtensions
{
	public static bool IsDatabaseExpression(this ExpressionType expressionType)
	{
		return (int)expressionType >= 1000;
	}

	public static string ResolveNodeTypeName(this Expression expression)
	{
		if (expression is DatabaseExpression d)
			return d.ExpressionType.ToString();
		else
			return expression.NodeType.ToString();
	}

	public static SelectExpression SetColumns(this SelectExpression select, IEnumerable<ColumnDeclaration> columns)
	{
		return new SelectExpression(select.Alias, columns.OrderBy(c => c.Name), select.From, select.Where, select.OrderBy, select.GroupBy, select.IsDistinct,
			 select.Skip, select.Take, select.IsReverse);
	}

	public static SelectExpression AddColumn(this SelectExpression select, ColumnDeclaration column)
	{
		var columns = new List<ColumnDeclaration>(select.Columns)
	  {
		  column
	  };

		return select.SetColumns(columns);
	}

	public static SelectExpression RemoveColumn(this SelectExpression select, ColumnDeclaration column)
	{
		var columns = new List<ColumnDeclaration>(select.Columns);

		columns.Remove(column);

		return select.SetColumns(columns);
	}

	public static string ResolveAvailableColumnName(this IList<ColumnDeclaration> columns, string baseName)
	{
		var name = baseName;
		var n = 0;

		while (!IsUniqueName(columns, name))
			name = baseName + n++;

		return name;
	}

	private static bool IsUniqueName(IList<ColumnDeclaration> columns, string name)
	{
		foreach (var col in columns)
		{
			if (string.Equals(col.Name, name, StringComparison.OrdinalIgnoreCase))
				return false;
		}

		return true;
	}

	public static ProjectionExpression AddOuterJoinTest(this ProjectionExpression proj, QueryLanguage language, Expression expression)
	{
		var colName = proj.Select.Columns.ResolveAvailableColumnName("Test");
		var colType = language.TypeSystem.ResolveColumnType(expression.Type);
		var newSource = proj.Select.AddColumn(new ColumnDeclaration(colName, expression, colType));
		var newProjector = new OuterJoinedExpression(new ColumnExpression(expression.Type, colType, newSource.Alias, colName), proj.Projector);

		return new ProjectionExpression(newSource, newProjector, proj.Aggregator);
	}

	public static SelectExpression SetDistinct(this SelectExpression select, bool isDistinct)
	{
		if (select.IsDistinct != isDistinct)
			return new SelectExpression(select.Alias, select.Columns, select.From, select.Where, select.OrderBy, select.GroupBy, isDistinct, select.Skip, select.Take, select.IsReverse);

		return select;
	}

	public static SelectExpression SetReverse(this SelectExpression select, bool isReverse)
	{
		if (select.IsReverse != isReverse)
			return new SelectExpression(select.Alias, select.Columns, select.From, select.Where, select.OrderBy, select.GroupBy, select.IsDistinct, select.Skip, select.Take, isReverse);

		return select;
	}

	public static SelectExpression SetWhere(this SelectExpression select, Expression? where)
	{
		if (where != select.Where)
			return new SelectExpression(select.Alias, select.Columns, select.From, where, select.OrderBy, select.GroupBy, select.IsDistinct, select.Skip, select.Take, select.IsReverse);

		return select;
	}

	public static SelectExpression SetOrderBy(this SelectExpression select, IEnumerable<OrderExpression> orderBy)
	{
		return new SelectExpression(select.Alias, select.Columns, select.From, select.Where, orderBy, select.GroupBy, select.IsDistinct, select.Skip, select.Take, select.IsReverse);
	}

	public static SelectExpression AddOrderExpression(this SelectExpression select, OrderExpression ordering)
	{
		var orderby = new List<OrderExpression>();

		if (select.OrderBy != null)
			orderby.AddRange(select.OrderBy);

		orderby.Add(ordering);

		return select.SetOrderBy(orderby);
	}

	public static SelectExpression RemoveOrderExpression(this SelectExpression select, OrderExpression ordering)
	{
		if (select.OrderBy != null && select.OrderBy.Count > 0)
		{
			var orderby = new List<OrderExpression>(select.OrderBy);

			orderby.Remove(ordering);

			return select.SetOrderBy(orderby);
		}

		return select;
	}

	public static SelectExpression SetGroupBy(this SelectExpression select, IEnumerable<Expression> groupBy)
	{
		return new SelectExpression(select.Alias, select.Columns, select.From, select.Where, select.OrderBy, groupBy, select.IsDistinct, select.Skip, select.Take, select.IsReverse);
	}

	public static SelectExpression AddGroupExpression(this SelectExpression select, Expression expression)
	{
		var groupby = new List<Expression>();

		if (select.GroupBy is not null)
			groupby.AddRange(select.GroupBy);

		groupby.Add(expression);

		return select.SetGroupBy(groupby);
	}

	public static SelectExpression RemoveGroupExpression(this SelectExpression select, Expression expression)
	{
		if (select.GroupBy is not null && select.GroupBy.Any())
		{
			var groupby = new List<Expression>(select.GroupBy);

			groupby.Remove(expression);

			return select.SetGroupBy(groupby);
		}

		return select;
	}

	public static SelectExpression SetSkip(this SelectExpression select, Expression? skip)
	{
		if (skip != select.Skip)
			return new SelectExpression(select.Alias, select.Columns, select.From, select.Where, select.OrderBy, select.GroupBy, select.IsDistinct, skip, select.Take, select.IsReverse);

		return select;
	}

	public static SelectExpression SetTake(this SelectExpression select, Expression? take)
	{
		if (take != select.Take)
			return new SelectExpression(select.Alias, select.Columns, select.From, select.Where, select.OrderBy, select.GroupBy, select.IsDistinct, select.Skip, take, select.IsReverse);

		return select;
	}

	public static SelectExpression AddRedundantSelect(this SelectExpression sel, QueryLanguage language, Alias newAlias)
	{
		var newColumns = from d in sel.Columns
							  let qt = d.Expression is ColumnExpression ? ((ColumnExpression)d.Expression).QueryType : language.TypeSystem.ResolveColumnType(d.Expression.Type)
							  select new ColumnDeclaration(d.Name, new ColumnExpression(d.Expression.Type, qt, newAlias, d.Name), qt);

		var newFrom = new SelectExpression(newAlias, sel.Columns, sel.From, sel.Where, sel.OrderBy, sel.GroupBy, sel.IsDistinct, sel.Skip, sel.Take, sel.IsReverse);

		return new SelectExpression(sel.Alias, newColumns, newFrom, null, null, null, false, null, null, false);
	}

	public static SelectExpression RemoveRedundantFrom(this SelectExpression select)
	{
		var fromSelect = select.From as SelectExpression;

		if (fromSelect is not null)
			return Subqueries.Remove(select, fromSelect);

		return select;
	}

	public static SelectExpression SetFrom(this SelectExpression select, Expression from)
	{
		if (select.From != from)
			return new SelectExpression(select.Alias, select.Columns, from, select.Where, select.OrderBy, select.GroupBy, select.IsDistinct, select.Skip, select.Take, select.IsReverse);

		return select;
	}
}
