using System.Linq.Expressions;

namespace Connected.Data.Expressions.Expressions;

public sealed class IsNullExpression(Expression expression)
		: DatabaseExpression(DatabaseExpressionType.IsNull, typeof(bool))
{
	public Expression Expression { get; } = expression;
}
