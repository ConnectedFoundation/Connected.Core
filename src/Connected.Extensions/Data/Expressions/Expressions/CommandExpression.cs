namespace Connected.Data.Expressions.Expressions;

public abstract class CommandExpression(DatabaseExpressionType eType, Type type)
		: DatabaseExpression(eType, type)
{
}