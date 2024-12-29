namespace Connected.Data.Expressions.Expressions;

public sealed class ExistsExpression : SubqueryExpression
{
	public ExistsExpression(SelectExpression select)
		 : base(DatabaseExpressionType.Exists, typeof(bool), select)
	{
	}
}
