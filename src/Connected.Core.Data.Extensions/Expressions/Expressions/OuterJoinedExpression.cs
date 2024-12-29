using System.Linq.Expressions;

namespace Connected.Data.Expressions.Expressions;

public sealed class OuterJoinedExpression : DatabaseExpression
{
	public OuterJoinedExpression(Expression test, Expression expression)
		 : base(DatabaseExpressionType.OuterJoined, expression.Type)
	{
		Test = test;
		Expression = expression;
	}

	public Expression Test { get; }
	public Expression Expression { get; }
}
