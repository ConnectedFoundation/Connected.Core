using Connected.Data.Expressions.Languages;
using System.Linq.Expressions;

namespace Connected.Data.Expressions.Expressions;

public sealed class VariableExpression(string name, Type type, DataType dataType)
		: Expression
{
	public string Name { get; } = name;
	public override Type Type { get; } = type;
	public DataType DataType { get; } = dataType;
	public override ExpressionType NodeType => (ExpressionType)DatabaseExpressionType.Variable;
}