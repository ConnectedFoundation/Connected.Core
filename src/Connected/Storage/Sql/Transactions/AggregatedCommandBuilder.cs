using Connected.Entities;

namespace Connected.Storage.Sql.Transactions;

internal class AggregatedCommandBuilder<TEntity>
	where TEntity : IEntity
{
	public static async Task<SqlStorageOperation?> Build(IStorage<TEntity> storage, TEntity entity, IEnumerable<string>? updatingProperties, CancellationToken cancel)
	{
		if (entity is not IEntity ie)
			throw new ArgumentException(null, nameof(entity));

		var result = ie.State switch
		{
			State.Add => await AggregatedCommandBuilder<TEntity>.BuildInsert(storage, ie, cancel),
			State.Update => await AggregatedCommandBuilder<TEntity>.BuildUpdate(storage, ie, updatingProperties, cancel),
			State.Delete => await AggregatedCommandBuilder<TEntity>.BuildDelete(storage, ie, cancel),
			_ => throw new NotSupportedException(),
		};

		if (storage.Variables is not null)
		{
			foreach (var variable in storage.Variables)
			{
				if (result.Variables.FirstOrDefault(f => string.Equals(f.Name, variable.Name, StringComparison.OrdinalIgnoreCase)) is IStorageVariable existing)
					continue;

				result.Variables.Add(variable);
			}
		}

		return result;
	}

	private static Task<SqlStorageOperation?> BuildInsert(IStorage<TEntity> storage, IEntity entity, CancellationToken cancel)
	{
		return new InsertCommandBuilder<TEntity>(storage).Build(entity, null, cancel);
	}

	private static Task<SqlStorageOperation?> BuildUpdate(IStorage<TEntity> storage, IEntity entity, IEnumerable<string>? updatingProperties, CancellationToken cancel)
	{
		return new UpdateCommandBuilder<TEntity>(storage).Build(entity, updatingProperties, cancel);
	}

	private static Task<SqlStorageOperation?> BuildDelete(IStorage<TEntity> storage, IEntity entity, CancellationToken cancel)
	{
		return new DeleteCommandBuilder<TEntity>(storage).Build(entity, null, cancel);
	}
}
