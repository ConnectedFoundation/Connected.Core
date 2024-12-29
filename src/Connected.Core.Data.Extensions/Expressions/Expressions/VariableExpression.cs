using System;
using System.Linq.Expressions;
using Connected.Data.Expressions.Languages;

namespace Connected.Data.Expressions.Expressions;

public sealed class VariableExpression : Expression
{
	public VariableExpression(string name, Type type, DataType dataType)
	{
		Name = name;
		Type = type;
		DataType = dataType;
	}

	public string Name { get; }
	public override Type Type { get; }
	public DataType DataType { get; }
	public override ExpressionType NodeType => (ExpressionType)DatabaseExpressionType.Variable;
}