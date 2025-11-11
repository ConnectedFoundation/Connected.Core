using Connected.Data.Expressions.Collections;
using System.Collections.ObjectModel;
using System.Linq.Expressions;

namespace Connected.Data.Expressions.Expressions;

public sealed class FunctionExpression
	: DatabaseExpression
{
	public FunctionExpression(Type type, string name, IEnumerable<Expression>? arguments)
		  : base(DatabaseExpressionType.Function, type)
	{
		Name = name;

		if (arguments is not null)
			Arguments = arguments.ToReadOnly();
	}

	public string Name { get; }
	public ReadOnlyCollection<Expression>? Arguments { get; }
}
