using Connected.Data.Expressions.Collections;
using Connected.Data.Expressions.Formatters;
using Connected.Data.Expressions.Translation;
using System.Collections.ObjectModel;
using System.Linq.Expressions;

namespace Connected.Data.Expressions.Expressions;

public sealed class SelectExpression(Alias alias, IEnumerable<ColumnDeclaration> columns, Expression? from, Expression? where,
		 IEnumerable<OrderExpression>? orderBy, IEnumerable<Expression>? groupBy, bool isDistinct, Expression? skip, Expression? take, bool isReverse)
		: AliasedExpression(DatabaseExpressionType.Select, typeof(void), alias)
{
	public SelectExpression(Alias alias, IEnumerable<ColumnDeclaration> columns, Expression? from, Expression? where, IEnumerable<OrderExpression>? orderBy, IEnumerable<Expression>? groupBy)
		 : this(alias, columns, from, where, orderBy, groupBy, false, null, null, false)
	{
	}

	public SelectExpression(Alias alias, IEnumerable<ColumnDeclaration> columns, Expression? from, Expression? where)
		 : this(alias, columns, from, where, null, null)
	{
	}

	public ReadOnlyCollection<ColumnDeclaration> Columns { get; } = columns.ToReadOnly();
	public Expression? From { get; } = from;
	public Expression? Where { get; } = where;
	public ReadOnlyCollection<OrderExpression>? OrderBy { get; } = orderBy?.ToReadOnly();
	public ReadOnlyCollection<Expression>? GroupBy { get; } = groupBy?.ToReadOnly();
	public bool IsDistinct { get; } = isDistinct;
	public Expression? Skip { get; } = skip;
	public Expression? Take { get; } = take;
	public bool IsReverse { get; } = isReverse;
	public string QueryText => SqlFormatter.Format(this);
}