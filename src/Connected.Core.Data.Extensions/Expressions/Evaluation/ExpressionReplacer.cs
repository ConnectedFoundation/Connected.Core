using Connected.Data.Expressions.Visitors;
using System.Linq.Expressions;

namespace Connected.Data.Expressions.Evaluation;

internal sealed class ExpressionReplacer : DatabaseVisitor
{
	private readonly Expression _searchFor;
	private readonly Expression _replaceWith;

	private ExpressionReplacer(Expression searchFor, Expression replaceWith)
	{
		_searchFor = searchFor;
		_replaceWith = replaceWith;
	}

	public static Expression Replace(Expression expression, Expression searchFor, Expression replaceWith)
	{
		return new ExpressionReplacer(searchFor, replaceWith).Visit(expression);
	}

	public static Expression ReplaceAll(Expression expression, Expression[] searchFor, Expression[] replaceWith)
	{
		for (int i = 0, n = searchFor.Length; i < n; i++)
			expression = Replace(expression, searchFor[i], replaceWith[i]);

		return expression;
	}

	protected override Expression? Visit(Expression? exp)
	{
		if (exp == _searchFor)
			return _replaceWith;

		return base.Visit(exp);
	}
}