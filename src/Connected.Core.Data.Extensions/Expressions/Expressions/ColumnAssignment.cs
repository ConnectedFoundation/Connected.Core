using System.Linq.Expressions;

namespace Connected.Data.Expressions.Expressions;

public sealed class ColumnAssignment
{
	public ColumnAssignment(ColumnExpression column, Expression expression)
	{
		Column = column;
		Expression = expression;
	}

	public ColumnExpression Column { get; }
	public Expression Expression { get; }
}

