using System;

namespace Connected.Data.Expressions.Expressions;

public sealed class ScalarExpression : SubqueryExpression
{
	public ScalarExpression(Type type, SelectExpression select)
		 : base(DatabaseExpressionType.Scalar, type, select)
	{
	}
}
