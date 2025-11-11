using System.Linq.Expressions;

namespace Connected.Data.Expressions.Expressions;

public enum OrderType
{
	Ascending,
	Descending
}

public sealed class OrderExpression(OrderType orderType, Expression expression)
{
	public OrderType OrderType { get; } = orderType;
	public Expression Expression { get; } = expression;
}
