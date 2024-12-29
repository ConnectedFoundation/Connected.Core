using System;

namespace Connected.Data.Expressions.Expressions;

public abstract class CommandExpression : DatabaseExpression
{
	protected CommandExpression(DatabaseExpressionType eType, Type type)
		 : base(eType, type)
	{
	}
}