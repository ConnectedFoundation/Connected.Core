using Connected.Data.Expressions.Translation;

namespace Connected.Data.Expressions.Expressions;

public sealed class TableExpression(Alias alias, Type entity, string schema, string name)
		: AliasedExpression(DatabaseExpressionType.Table, typeof(void), alias)
{
	public Type Entity { get; } = entity;
	public string Name { get; } = name;
	public string Schema { get; } = schema;

	public override string ToString()
	{
		return $"T({Schema}.{Name})";
	}
}