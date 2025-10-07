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

public sealed class JoinExpression : DatabaseExpression
{
	public JoinExpression(JoinType joinType, Expression left, Expression right, Expression? condition)
		 : base(DatabaseExpressionType.Join, typeof(void))
	{
		Join = joinType;
		Left = left;
		Right = right;
		Condition = condition;
	}

	public JoinType Join { get; }
	public Expression Left { get; }
	public Expression Right { get; }
	public new Expression? Condition { get; }
}