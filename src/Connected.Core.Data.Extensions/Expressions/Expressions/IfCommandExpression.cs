using System.Linq.Expressions;

namespace Connected.Data.Expressions.Expressions;

public sealed class IfCommandExpression : CommandExpression
{
	public IfCommandExpression(Expression check, Expression ifTrue, Expression ifFalse)
		 : base(DatabaseExpressionType.If, ifTrue.Type)
	{
		Check = check;
		IfTrue = ifTrue;
		IfFalse = ifFalse;
	}

	public Expression Check { get; }
	public Expression IfTrue { get; }
	public Expression IfFalse { get; }
}
