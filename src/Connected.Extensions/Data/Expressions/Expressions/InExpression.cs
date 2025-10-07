using System.Collections.ObjectModel;
using System.Collections.Generic;
using System.Linq.Expressions;
using Connected.Data.Expressions.Collections;

namespace Connected.Data.Expressions.Expressions;

public sealed class InExpression : SubqueryExpression
{
	public InExpression(Expression expression, SelectExpression select)
		  : base(DatabaseExpressionType.In, typeof(bool), select)
	{
		Expression = expression;
	}

	public InExpression(Expression expression, IEnumerable<Expression> values)
		  : base(DatabaseExpressionType.In, typeof(bool), null)
	{
		Expression = expression;
		Values = values.ToReadOnly();
	}

	public Expression Expression { get; }
	public ReadOnlyCollection<Expression>? Values { get; }
}
