using Connected.Reflection;
using System.Linq.Expressions;

namespace Connected.Data.Expressions.Expressions;

internal static class ExpressionExtensions
{
	public static Expression Equal(this Expression left, Expression right)
	{
		ConvertExpressions(ref left, ref right);

		return Expression.Equal(left, right);
	}

	public static Expression NotEqual(this Expression left, Expression right)
	{
		ConvertExpressions(ref left, ref right);

		return Expression.NotEqual(left, right);
	}

	public static Expression GreaterThan(this Expression left, Expression right)
	{
		ConvertExpressions(ref left, ref right);

		return Expression.GreaterThan(left, right);
	}

	public static Expression GreaterThanOrEqual(this Expression left, Expression right)
	{
		ConvertExpressions(ref left, ref right);

		return Expression.GreaterThanOrEqual(left, right);
	}

	public static Expression LessThan(this Expression left, Expression right)
	{
		ConvertExpressions(ref left, ref right);

		return Expression.LessThan(left, right);
	}

	public static Expression LessThanOrEqual(this Expression left, Expression right)
	{
		ConvertExpressions(ref left, ref right);

		return Expression.LessThanOrEqual(left, right);
	}

	public static Expression And(this Expression left, Expression right)
	{
		ConvertExpressions(ref left, ref right);

		return Expression.And(left, right);
	}

	public static Expression Or(this Expression left, Expression right)
	{
		ConvertExpressions(ref left, ref right);

		return Expression.Or(left, right);
	}

	public static Expression Binary(this Expression left, ExpressionType op, Expression right)
	{
		ConvertExpressions(ref left, ref right);

		return Expression.MakeBinary(op, left, right);
	}

	private static void ConvertExpressions(ref Expression left, ref Expression right)
	{
		if (left.Type != right.Type)
		{
			var isNullable1 = left.Type.IsNullable();
			var isNullable2 = right.Type.IsNullable();

			if (isNullable1 || isNullable2)
			{
				if (Nullables.GetNonNullableType(left.Type) == Nullables.GetNonNullableType(right.Type))
				{
					if (!isNullable1)
						left = Expression.Convert(left, right.Type);
					else if (!isNullable2)
						right = Expression.Convert(right, left.Type);
				}
			}
		}
	}

	public static Expression[] Split(this Expression expression, params ExpressionType[] binarySeparators)
	{
		var list = new List<Expression>();

		Split(expression, list, binarySeparators);

		return [.. list];
	}

	private static void Split(Expression expression, List<Expression> list, ExpressionType[] binarySeparators)
	{
		if (expression is not null)
		{
			if (binarySeparators.Contains(expression.NodeType))
			{
				if (expression is BinaryExpression bex)
				{
					Split(bex.Left, list, binarySeparators);
					Split(bex.Right, list, binarySeparators);
				}
			}
			else
				list.Add(expression);
		}
	}

	public static Expression? Join(this IEnumerable<Expression> list, ExpressionType binarySeparator)
	{
		var array = list.ToArray();

		if (array.Length != 0)
			return array.Aggregate((x1, x2) => Expression.MakeBinary(binarySeparator, x1, x2));

		return null;
	}
}
