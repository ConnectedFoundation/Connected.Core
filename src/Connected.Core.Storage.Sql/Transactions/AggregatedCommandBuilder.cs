using Connected.Entities;
using System.Collections.Immutable;

namespace Connected.Storage.Sql.Transactions;

internal class AggregatedCommandBuilder<TEntity>
{
	public async Task<SqlStorageOperation?> Build(TEntity entity, CancellationToken cancel)
	{
		if (entity is not IEntity ie)
			throw new ArgumentException(nameof(entity));

		switch (ie.State)
		{
			case State.Add:
				return await BuildInsert(ie, cancel);
			case State.Update:
				return await BuildUpdate(ie, cancel);
			case State.Delete:
				return await BuildDelete(ie, cancel);
			default:
				throw new NotSupportedException();
		}
	}

	public async Task<List<SqlStorageOperation>> Build(ImmutableArray<TEntity> entities, CancellationToken cancel)
	{
		var result = new List<SqlStorageOperation>();

		foreach (var entity in entities)
		{
			var operation = await Build(entity, cancel);

			if (operation is not null)
				result.Add(operation);
		}

		return result;
	}

	private Task<SqlStorageOperation?> BuildInsert(IEntity entity, CancellationToken cancel)
	{
		return new InsertCommandBuilder().Build(entity, cancel);
	}

	private Task<SqlStorageOperation?> BuildUpdate(IEntity entity, CancellationToken cancel)
	{
		return new UpdateCommandBuilder().Build(entity, cancel);
	}

	private Task<SqlStorageOperation?> BuildDelete(IEntity entity, CancellationToken cancel)
	{
		return new DeleteCommandBuilder().Build(entity, cancel);
	}
}
