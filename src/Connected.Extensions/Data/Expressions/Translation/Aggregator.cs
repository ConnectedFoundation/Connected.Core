using Connected.Data.Expressions.Collections;
using Connected.Reflection;
using System.Linq.Expressions;
using System.Reflection;

namespace Connected.Data.Expressions.Translation;

public static class Aggregator
{
	public static LambdaExpression? GetAggregator(Type expectedType, Type actualType)
	{
		var actualElementType = Enumerables.GetEnumerableElementType(actualType);

		if (!expectedType.GetTypeInfo().IsAssignableFrom(actualType.GetTypeInfo()))
		{
			var expectedElementType = Enumerables.GetEnumerableElementType(expectedType);
			var p = Expression.Parameter(actualType, "p");
			Expression? body = null;

			if (expectedType.GetTypeInfo().IsAssignableFrom(actualElementType.GetTypeInfo()))
				body = Expression.Call(typeof(Enumerable), "SingleOrDefault", new Type[] { actualElementType }, p);
			else if (expectedType.GetTypeInfo().IsGenericType && (expectedType == typeof(IQueryable) || expectedType == typeof(IOrderedQueryable) || expectedType.GetGenericTypeDefinition() == typeof(IQueryable<>) || expectedType.GetGenericTypeDefinition() == typeof(IOrderedQueryable<>)))
			{
				body = Expression.Call(typeof(Queryable), "AsQueryable", new Type[] { expectedElementType }, CoerceElement(expectedElementType, p));

				if (body.Type != expectedType)
					body = Expression.Convert(body, expectedType);
			}
			else if (expectedType.IsArray && expectedType.GetArrayRank() == 1)
				body = Expression.Call(typeof(Enumerable), "ToArray", new Type[] { expectedElementType }, CoerceElement(expectedElementType, p));
			else if (expectedType.GetTypeInfo().IsGenericType && expectedType.GetGenericTypeDefinition().GetTypeInfo().IsAssignableFrom(typeof(IList<>).GetTypeInfo()))
			{
				var gt = typeof(DeferredList<>).MakeGenericType(expectedType.GetTypeInfo().GenericTypeArguments);
				var cn = Types.FindConstructor(gt, [typeof(IEnumerable<>).MakeGenericType(expectedType.GetTypeInfo().GenericTypeArguments)]);

				body = Expression.New(cn, CoerceElement(expectedElementType, p));
			}
			else if (expectedType.GetTypeInfo().IsAssignableFrom(typeof(List<>).MakeGenericType(actualElementType).GetTypeInfo()))
				body = Expression.Call(typeof(Enumerable), "ToList", [expectedElementType], CoerceElement(expectedElementType, p));
			else
			{
				var ci = Types.FindConstructor(expectedType, new Type[] { actualType });

				if (ci is not null)
					body = Expression.New(ci, p);
			}
			if (body is not null)
				return Expression.Lambda(body, p);
		}

		return null;
	}

	private static Expression CoerceElement(Type expectedElementType, Expression expression)
	{
		var elementType = Enumerables.GetEnumerableElementType(expression.Type);

		if (expectedElementType != elementType && (expectedElementType.GetTypeInfo().IsAssignableFrom(elementType.GetTypeInfo()) || elementType.GetTypeInfo().IsAssignableFrom(expectedElementType.GetTypeInfo())))
			return Expression.Call(typeof(Enumerable), "Cast", new Type[] { expectedElementType }, expression);

		return expression;
	}
}
