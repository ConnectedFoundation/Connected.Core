using System.Linq.Expressions;

namespace Connected.Data.Expressions.Expressions;

public sealed class BetweenExpression : DatabaseExpression
{
	public BetweenExpression(Expression expression, Expression lower, Expression upper)
		 : base(DatabaseExpressionType.Between, expression.Type)
	{
		Expression = expression;
		Lower = lower;
		Upper = upper;
	}

	public Expression Expression { get; }
	public Expression Lower { get; }
	public Expression Upper { get; }
}