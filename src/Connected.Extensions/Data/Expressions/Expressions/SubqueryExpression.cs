namespace Connected.Data.Expressions.Expressions;

public class SubqueryExpression
	: DatabaseExpression
{
	protected SubqueryExpression(DatabaseExpressionType eType, Type type, SelectExpression? select)
		 : base(eType, type)
	{
		Select = select;
	}

	public SelectExpression? Select { get; }
}
