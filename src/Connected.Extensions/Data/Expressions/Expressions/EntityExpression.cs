using System;
using System.Linq.Expressions;

namespace Connected.Data.Expressions.Expressions;

public sealed class EntityExpression : DatabaseExpression
{
	public EntityExpression(Type entityType, Expression expression)
		: base(DatabaseExpressionType.Entity, expression.Type)
	{
		EntityType = entityType;
		Expression = expression;
	}

	public Type EntityType { get; }
	public Expression Expression { get; }
}