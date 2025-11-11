using System.Linq.Expressions;

namespace Connected.Data.Expressions.Expressions;

public sealed class IfCommandExpression(Expression check, Expression ifTrue, Expression ifFalse)
		: CommandExpression(DatabaseExpressionType.If, ifTrue.Type)
{
	public Expression Check { get; } = check;
	public Expression IfTrue { get; } = ifTrue;
	public Expression IfFalse { get; } = ifFalse;
}
