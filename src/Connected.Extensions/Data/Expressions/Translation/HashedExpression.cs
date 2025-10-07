using System.Linq.Expressions;
using System;
using Connected.Data.Expressions.Comparers;

namespace Connected.Data.Expressions.Translation;

internal readonly struct HashedExpression : IEquatable<HashedExpression>
{
	private readonly Expression _expression;
	private readonly int _hashCode;

	public HashedExpression(Expression expression)
	{
		_expression = expression;
		_hashCode = Hasher.ComputeHash(expression);
	}

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