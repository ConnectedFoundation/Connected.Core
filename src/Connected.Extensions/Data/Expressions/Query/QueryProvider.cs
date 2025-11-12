using System.Linq.Expressions;

namespace Connected.Data.Expressions.Query;

public abstract class QueryProvider
	: Middleware, IQueryProvider
{
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

	public object? Execute(Expression expression)
	{
		return OnExecute(expression);
	}

	public TResult Execute<TResult>(Expression expression)
	{
		return (TResult)OnExecute(expression);
	}

	protected abstract object OnExecute(Expression expression);
}
