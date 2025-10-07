using Connected.Data.Expressions.Visitors;
using System.Linq.Expressions;

namespace Connected.Data.Expressions.Translation;

internal sealed class Hasher : DatabaseVisitor
{
	private int _hc;

	internal static int ComputeHash(Expression expression)
	{
		var hasher = new Hasher();

		hasher.Visit(expression);

		return hasher._hc;
	}

	protected override Expression VisitConstant(ConstantExpression expression)
	{
		_hc += expression.Value is not null ? expression.Value.GetHashCode() : 0;

		return expression;
	}
}
