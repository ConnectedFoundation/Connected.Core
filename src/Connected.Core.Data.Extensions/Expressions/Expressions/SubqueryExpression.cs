using System;

namespace Connected.Data.Expressions.Expressions;

public class SubqueryExpression : DatabaseExpression
{
	protected SubqueryExpression(DatabaseExpressionType eType, Type type, SelectExpression? select)
		 : base(eType, type)
	{
		System.Diagnostics.Debug.Assert(eType == DatabaseExpressionType.Scalar || eType == DatabaseExpressionType.Exists || eType == DatabaseExpressionType.In);

		Select = select;
	}

	public SelectExpression? Select { get; }
}
