using System.Linq.Expressions;

namespace Connected.Data.Expressions.Expressions;

public enum JoinType
{
	CrossJoin = 0,
	InnerJoin = 1,
	CrossApply = 2,
	OuterApply = 3,
	LeftOuter = 4,
	SingletonLeftOuter = 5
}

public sealed class JoinExpression(JoinType joinType, Expression left, Expression right, Expression? condition)
		: DatabaseExpression(DatabaseExpressionType.Join, typeof(void))
{
	public JoinType Join { get; } = joinType;
	public Expression Left { get; } = left;
	public Expression Right { get; } = right;
	public new Expression? Condition { get; } = condition;
}