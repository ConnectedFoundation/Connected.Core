namespace Connected.Data.Expressions.Expressions;

public sealed class ScalarExpression(Type type, SelectExpression select)
		: SubqueryExpression(DatabaseExpressionType.Scalar, type, select)
{
}
