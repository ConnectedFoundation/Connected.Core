using System.Linq.Expressions;

namespace Connected.Data.Expressions.Expressions;

public sealed class OuterJoinedExpression(Expression test, Expression expression)
	: DatabaseExpression(DatabaseExpressionType.OuterJoined, expression.Type)
{
	public Expression Test { get; } = test;
	public Expression Expression { get; } = expression;
}
