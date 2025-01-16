using System.Linq.Expressions;

namespace Connected.Data.Expressions.Query;

public abstract class QueryProvider : Middleware, IQueryProvider
{
	public IQueryable CreateQuery(Expression expression)
	{
		var type = expression.Type.GetElementType();
		var generic = typeof(EntityQuery<>).MakeGenericType(new Type[] { type });

		if (generic is null)
			throw new NullReferenceException(nameof(type));

		var instance = Activator.CreateInstance(generic, new object[] { this, expression }) as IQueryable;

		if (instance is null)
			throw new NullReferenceException(nameof(type));

		return instance;
	}

	public IQueryable<TElement> CreateQuery<TElement>(Expression expression)
	{
		return new EntityQuery<TElement>(this, expression);
	}

	public object? Execute(Expression expression)
	{
		return OnExecute(expression);
	}

	public TResult Execute<TResult>(Expression expression)
	{
		return (TResult)OnExecute(expression);
	}

	protected virtual object? OnExecute(Expression expression)
	{
		return default;
	}
}
