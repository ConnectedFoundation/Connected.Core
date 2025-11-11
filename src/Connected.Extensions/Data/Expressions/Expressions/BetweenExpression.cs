using System.Linq.Expressions;

namespace Connected.Data.Expressions.Expressions;

public sealed class BetweenExpression(Expression expression, Expression lower, Expression upper)
		: DatabaseExpression(DatabaseExpressionType.Between, expression.Type)
{
	public Expression Expression { get; } = expression;
	public Expression Lower { get; } = lower;
	public Expression Upper { get; } = upper;
}