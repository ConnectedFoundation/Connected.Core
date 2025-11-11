using System.Linq.Expressions;

namespace Connected.Data.Expressions.Expressions;

public sealed class EntityExpression(Type entityType, Expression expression)
		: DatabaseExpression(DatabaseExpressionType.Entity, expression.Type)
{
	public Type EntityType { get; } = entityType;
	public Expression Expression { get; } = expression;
}