using System.Linq.Expressions;

namespace Connected.Data.Expressions.Expressions;

public sealed class NamedValueExpression : DatabaseExpression
{
	public NamedValueExpression(string name, Languages.DataType dataType, Expression value)
		 : base(DatabaseExpressionType.NamedValue, value.Type)
	{
		if (name is null)
			throw new ArgumentNullException(nameof(name));

		if (value is null)
			throw new ArgumentNullException(nameof(value));

		Name = name;
		DataType = dataType;
		Value = value;
	}

	public string Name { get; }
	public Languages.DataType DataType { get; }
	public Expression Value { get; }
}