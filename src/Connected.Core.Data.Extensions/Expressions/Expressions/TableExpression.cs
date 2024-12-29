using Connected.Data.Expressions.Translation;
using System;

namespace Connected.Data.Expressions.Expressions;

public sealed class TableExpression : AliasedExpression
{
	public TableExpression(Alias alias, Type entity, string schema, string name)
		: base(DatabaseExpressionType.Table, typeof(void), alias)
	{
		Entity = entity;
		Name = name;
		Schema = schema;
	}

	public Type Entity { get; }
	public string Name { get; }
	public string Schema { get; }

	public override string ToString()
	{
		return $"T({Schema}.{Name})";
	}
}