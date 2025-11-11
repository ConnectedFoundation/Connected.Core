namespace Connected.Data.Expressions.Expressions;

public sealed class ExistsExpression(SelectExpression select)
		: SubqueryExpression(DatabaseExpressionType.Exists, typeof(bool), select)
{
}
