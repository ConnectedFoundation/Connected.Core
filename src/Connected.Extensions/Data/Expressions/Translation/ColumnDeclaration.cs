using System;
using System.Linq.Expressions;
using Connected.Data.Expressions.Languages;

namespace Connected.Data.Expressions.Translation;

public sealed class ColumnDeclaration
{
	public ColumnDeclaration(string name, Expression expression, DataType dataType)
	{
		if (name is null)
			throw new ArgumentNullException(nameof(name));

		if (expression is null)
			throw new ArgumentNullException(nameof(expression));

		if (dataType is null)
			throw new ArgumentNullException(nameof(dataType));

		Name = name;
		Expression = expression;
		DataType = dataType;
	}

	public string Name { get; }
	public Expression Expression { get; }
	public DataType DataType { get; }
}
