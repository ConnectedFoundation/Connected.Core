using System.Linq.Expressions;

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
		var type = expression.Type.GetElementType() ?? throw new NullReferenceException(SR.ErrCannotResolveElementType);
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
		return (TResult)(await OnExecute(expression) ?? throw new NullReferenceException(nameof(expression)));
	}

	protected abstract Task<object?> OnExecute(Expression expression);
}
