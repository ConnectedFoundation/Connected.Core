using Connected.Entities;
using System.Collections.Immutable;

namespace Connected.Storage.Sql.Transactions;

internal class AggregatedCommandBuilder<TEntity>
{
	public static async Task<SqlStorageOperation?> Build(TEntity entity, CancellationToken cancel)
	{
		if (entity is not IEntity ie)
			throw new ArgumentException(null, nameof(entity));

		return ie.State switch
		{
			State.Add => await AggregatedCommandBuilder<TEntity>.BuildInsert(ie, cancel),
			State.Update => await AggregatedCommandBuilder<TEntity>.BuildUpdate(ie, cancel),
			State.Delete => await AggregatedCommandBuilder<TEntity>.BuildDelete(ie, cancel),
			_ => throw new NotSupportedException(),
		};
	}

	public static async Task<List<SqlStorageOperation>> Build(ImmutableArray<TEntity> entities, CancellationToken cancel)
	{
		var result = new List<SqlStorageOperation>();

		foreach (var entity in entities)
		{
			var operation = await AggregatedCommandBuilder<TEntity>.Build(entity, cancel);

			if (operation is not null)
				result.Add(operation);
		}

		return result;
	}

	private static Task<SqlStorageOperation?> BuildInsert(IEntity entity, CancellationToken cancel)
	{
		return new InsertCommandBuilder().Build(entity, cancel);
	}

	private static Task<SqlStorageOperation?> BuildUpdate(IEntity entity, CancellationToken cancel)
	{
		return new UpdateCommandBuilder().Build(entity, cancel);
	}

	private static Task<SqlStorageOperation?> BuildDelete(IEntity entity, CancellationToken cancel)
	{
		return new DeleteCommandBuilder().Build(entity, cancel);
	}
}
