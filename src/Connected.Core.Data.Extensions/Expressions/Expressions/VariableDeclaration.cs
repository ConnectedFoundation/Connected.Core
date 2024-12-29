using System.Linq.Expressions;
using Connected.Data.Expressions.Languages;

namespace Connected.Data.Expressions.Expressions;

public class VariableDeclaration
{
	public VariableDeclaration(string name, DataType dataType, Expression expression)
	{
		Name = name;
		DataType = dataType;
		Expression = expression;
	}

	public string Name { get; }
	public DataType DataType { get; }
	public Expression Expression { get; }
}
