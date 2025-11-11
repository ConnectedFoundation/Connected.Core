using Connected.Data.Expressions.Languages;
using System.Linq.Expressions;

namespace Connected.Data.Expressions.Expressions;

public class VariableDeclaration(string name, DataType dataType, Expression expression)
{
	public string Name { get; } = name;
	public DataType DataType { get; } = dataType;
	public Expression Expression { get; } = expression;
}
