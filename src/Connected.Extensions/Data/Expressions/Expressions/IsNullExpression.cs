using System.Linq.Expressions;

namespace Connected.Data.Expressions.Expressions;

public sealed class IsNullExpression : DatabaseExpression
{
	public IsNullExpression(Expression expression)
		 : base(DatabaseExpressionType.IsNull, typeof(bool))
	{
		Expression = expression;
	}

	public Expression Expression { get; }
}
