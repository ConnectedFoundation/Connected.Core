using System.Collections.ObjectModel;
using System.Collections.Generic;
using System.Linq.Expressions;
using Connected.Data.Expressions.Collections;
using Connected.Data.Expressions.Formatters;
using Connected.Data.Expressions.Translation;

namespace Connected.Data.Expressions.Expressions;

public sealed class SelectExpression : AliasedExpression
{
	public SelectExpression(Alias alias, IEnumerable<ColumnDeclaration> columns, Expression from, Expression? where,
		 IEnumerable<OrderExpression>? orderBy, IEnumerable<Expression>? groupBy, bool isDistinct, Expression? skip, Expression? take, bool isReverse)
		 : base(DatabaseExpressionType.Select, typeof(void), alias)
	{
		Columns = columns.ToReadOnly();
		IsDistinct = isDistinct;
		From = from;
		Where = where;
		OrderBy = orderBy?.ToReadOnly();
		GroupBy = groupBy?.ToReadOnly();
		Take = take;
		Skip = skip;
		IsReverse = isReverse;
	}

	public SelectExpression(Alias alias, IEnumerable<ColumnDeclaration> columns, Expression from, Expression? where, IEnumerable<OrderExpression>? orderBy, IEnumerable<Expression>? groupBy)
		 : this(alias, columns, from, where, orderBy, groupBy, false, null, null, false)
	{
	}

	public SelectExpression(Alias alias, IEnumerable<ColumnDeclaration> columns, Expression from, Expression? where)
		 : this(alias, columns, from, where, null, null)
	{
	}

	public ReadOnlyCollection<ColumnDeclaration> Columns { get; }
	public Expression From { get; }
	public Expression? Where { get; }
	public ReadOnlyCollection<OrderExpression>? OrderBy { get; }
	public ReadOnlyCollection<Expression>? GroupBy { get; }
	public bool IsDistinct { get; }
	public Expression? Skip { get; }
	public Expression? Take { get; }
	public bool IsReverse { get; }
	public string QueryText => SqlFormatter.Format(this);
}