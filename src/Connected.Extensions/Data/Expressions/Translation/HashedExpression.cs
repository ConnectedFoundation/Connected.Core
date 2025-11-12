using Connected.Data.Expressions.Comparers;
using System.Linq.Expressions;

namespace Connected.Data.Expressions.Translation;

internal readonly struct HashedExpression(Expression expression)
	: IEquatable<HashedExpression>
{
	private readonly Expression _expression = expression;
	private readonly int _hashCode = Hasher.ComputeHash(expression);

	public override bool Equals(object? obj)
	{
		if (obj is not HashedExpression)
			return false;

		return Equals((HashedExpression)obj);
	}

	public bool Equals(HashedExpression other)
	{
		return _hashCode == other._hashCode && DatabaseComparer.AreEqual(_expression, other._expression);
	}

	public override int GetHashCode()
	{
		return _hashCode;
	}
}