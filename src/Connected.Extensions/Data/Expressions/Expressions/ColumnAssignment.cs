using System.Linq.Expressions;

namespace Connected.Data.Expressions.Expressions;

public sealed class ColumnAssignment(ColumnExpression column, Expression expression)
{
	public ColumnExpression Column { get; } = column;
	public Expression Expression { get; } = expression;
}

