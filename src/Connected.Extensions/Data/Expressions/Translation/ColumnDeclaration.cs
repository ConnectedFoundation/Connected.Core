using Connected.Data.Expressions.Languages;
using System.Linq.Expressions;

namespace Connected.Data.Expressions.Translation;

public sealed class ColumnDeclaration
{
	public ColumnDeclaration(string name, Expression expression, DataType dataType)
	{
		ArgumentNullException.ThrowIfNull(name);
		ArgumentNullException.ThrowIfNull(expression);
		ArgumentNullException.ThrowIfNull(dataType);

		Name = name;
		Expression = expression;
		DataType = dataType;
	}

	public string Name { get; }
	public Expression Expression { get; }
	public DataType DataType { get; }
}
