using System.Linq.Expressions;

namespace Connected.Data.Expressions.Expressions;

public enum OrderType
{
	Ascending,
	Descending
}

public sealed class OrderExpression
{
	public OrderExpression(OrderType orderType, Expression expression)
	{
		OrderType = orderType;
		Expression = expression;
	}

	public OrderType OrderType { get; }
	public Expression Expression { get; }
}
