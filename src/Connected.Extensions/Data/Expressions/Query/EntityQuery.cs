using System.Collections;
using System.Collections.Immutable;
using System.Linq.Expressions;

namespace Connected.Data.Expressions.Query;

internal sealed class EntityQuery<TEntity>(IQueryProvider provider, Expression expression)
	: IQueryable<TEntity>, IAsyncEnumerable<TEntity>, IOrderedQueryable<TEntity>
{
	public Type ElementType => typeof(TEntity);

	public Expression Expression { get; } = expression;

	public IQueryProvider Provider { get; } = provider;

	public async IAsyncEnumerator<TEntity> GetAsyncEnumerator(CancellationToken cancellationToken = default)
	{
		var result = Provider.Execute<IImmutableList<TEntity>>(Expression);

		await Task.CompletedTask;

		if (result is IEnumerable en)
		{
			var enumerator = en.GetEnumerator();

			while (enumerator.MoveNext())
				yield return (TEntity)enumerator.Current;
		}
	}

	public IEnumerator<TEntity> GetEnumerator()
	{
		var result = Provider.Execute(Expression) ?? throw new NullReferenceException(SR.ErrExpectedEnumerator);

		if (result is not IEnumerable<TEntity> en)
			throw new NullReferenceException(SR.ErrExpectedEnumerator);

		return en.GetEnumerator();
	}

	IEnumerator IEnumerable.GetEnumerator()
	{
		var result = Provider.Execute(Expression) ?? throw new NullReferenceException(SR.ErrExpectedEnumerator);

		if (result is not IEnumerable en)
			throw new NullReferenceException(SR.ErrExpectedEnumerator);

		return en.GetEnumerator();
	}
}
