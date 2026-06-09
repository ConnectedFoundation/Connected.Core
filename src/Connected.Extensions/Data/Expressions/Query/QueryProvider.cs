using Connected.Reflection;
using System.Collections;
using System.Collections.Immutable;
using System.Linq.Expressions;
using System.Reflection;

namespace Connected.Data.Expressions.Query;

public abstract class QueryProvider
	: Middleware, IQueryProvider
{
	IQueryable IQueryProvider.CreateQuery(Expression expression)
	{
		return CreateQuery(expression);
	}

	IQueryable<TElement> IQueryProvider.CreateQuery<TElement>(Expression expression)
	{
		return CreateQuery<TElement>(expression);
	}

	object? IQueryProvider.Execute(Expression expression)
	{
		return Execute(expression).GetAwaiter().GetResult();
	}

	TResult IQueryProvider.Execute<TResult>(Expression expression)
	{
		return Execute<TResult>(expression).GetAwaiter().GetResult();
	}

	public IQueryable CreateQuery(Expression expression)
	{
		var type = expression.Type.GetElementType();

		if (type == null && expression.Type.IsGenericType && expression.Type.GetGenericTypeDefinition() == typeof(IQueryable<>))
			type = expression.Type.GenericTypeArguments[0];
			
		if(type == null)
			throw new NullReferenceException(SR.ErrCannotResolveElementType);

		var generic = typeof(EntityQuery<>).MakeGenericType([type]) ?? throw new NullReferenceException(nameof(type));

		if (Activator.CreateInstance(generic, [this, expression]) is not IQueryable instance)
			throw new NullReferenceException(nameof(type));

		return instance;
	}

	public IQueryable<TElement> CreateQuery<TElement>(Expression expression)
	{
		return new EntityQuery<TElement>(this, expression);
	}

	public virtual async Task<object?> Execute(Expression expression)
	{
		return await OnExecute(expression);
	}

	public virtual async Task<TResult> Execute<TResult>(Expression expression)
	{
		var result = await OnExecute(expression) ?? throw new NullReferenceException(nameof(expression));

		if (result is IEnumerable enumerable && typeof(TResult).IsEnumerable())
			return (TResult)ConvertCollection(enumerable, typeof(TResult));

		return (TResult)result;
	}

	protected abstract Task<object?> OnExecute(Expression expression);

	public static object ConvertCollection(object source, Type targetType)
	{
		if (source is null)
			throw new ArgumentNullException(nameof(source));

		Type? elementType = GetEnumerableElementType(targetType);

		if (elementType is null)
			throw new InvalidOperationException($"Target type {targetType} is not enumerable.");

		// Enumerable.Cast<G>(source)
		MethodInfo castMethod = typeof(Enumerable)
			 .GetMethod(nameof(Enumerable.Cast))!
			 .MakeGenericMethod(elementType);

		object castedEnumerable = castMethod.Invoke(null, new[] { source })!;

		// If target is IEnumerable<G>, we are done.
		if (targetType.IsAssignableFrom(castedEnumerable.GetType()))
			return castedEnumerable;

		// If target is IImmutableList<G>, materialize with ToImmutableList<G>()
		if (IsGenericType(targetType, typeof(IImmutableList<>)))
		{
			MethodInfo toImmutableListMethod = typeof(ImmutableList)
				 .GetMethods()
				 .Single(m =>
					  m.Name == nameof(ImmutableList.ToImmutableList) &&
					  m.IsGenericMethodDefinition &&
					  m.GetParameters().Length == 1)
				 .MakeGenericMethod(elementType);

			return toImmutableListMethod.Invoke(null, new[] { castedEnumerable })!;
		}

		throw new NotSupportedException($"Conversion to {targetType} is not supported.");
	}

	private static Type? GetEnumerableElementType(Type type)
	{
		if (type.IsGenericType &&
			 type.GetGenericTypeDefinition() == typeof(IEnumerable<>))
		{
			return type.GetGenericArguments()[0];
		}

		return type
			 .GetInterfaces()
			 .Where(i =>
				  i.IsGenericType &&
				  i.GetGenericTypeDefinition() == typeof(IEnumerable<>))
			 .Select(i => i.GetGenericArguments()[0])
			 .FirstOrDefault();
	}

	private static bool IsGenericType(Type type, Type genericTypeDefinition)
	{
		if (type.IsGenericType &&
			 type.GetGenericTypeDefinition() == genericTypeDefinition)
		{
			return true;
		}

		return type
			 .GetInterfaces()
			 .Any(i =>
				  i.IsGenericType &&
				  i.GetGenericTypeDefinition() == genericTypeDefinition);
	}
}
