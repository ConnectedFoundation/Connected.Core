using Connected.Data.Expressions.Collections;
using System.Collections.ObjectModel;
using System.Linq.Expressions;

namespace Connected.Data.Expressions.Expressions;

public sealed class InExpression
	: SubqueryExpression
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
