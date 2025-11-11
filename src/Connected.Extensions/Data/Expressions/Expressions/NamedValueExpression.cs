using System.Linq.Expressions;

namespace Connected.Data.Expressions.Expressions;

public sealed class NamedValueExpression(string name, Languages.DataType dataType, Expression value)
		: DatabaseExpression(DatabaseExpressionType.NamedValue, value.Type)
{
	public string Name { get; } = name;
	public Languages.DataType DataType { get; } = dataType;
	public Expression Value { get; } = value;
}