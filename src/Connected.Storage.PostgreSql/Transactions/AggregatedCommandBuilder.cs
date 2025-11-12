using Connected.Entities;
using System.Collections.Immutable;

namespace Connected.Storage.PostgreSql.Transactions;

/// <summary>
/// Aggregates and builds PostgreSQL storage operations for entity collections.
/// </summary>
/// <typeparam name="TEntity">The entity type to build operations for.</typeparam>
/// <remarks>
/// This class serves as a factory for creating INSERT, UPDATE, and DELETE commands based on
/// entity state. It routes entities to the appropriate command builder (InsertCommandBuilder,
/// UpdateCommandBuilder, or DeleteCommandBuilder) based on their State property. The class
/// supports both single entity operations and batch operations for collections of entities.
/// All generated commands use PostgreSQL-specific syntax including double-quoted identifiers
/// and RETURNING clauses for identity columns. The aggregation pattern allows for efficient
/// batch processing of multiple entities with different operation types.
/// </remarks>
internal sealed class AggregatedCommandBuilder<TEntity>
{
	/// <summary>
	/// Builds a storage operation for a single entity based on its state.
	/// </summary>
	/// <param name="entity">The entity to build the operation for.</param>
	/// <param name="cancel">Cancellation token for the operation.</param>
	/// <returns>A <see cref="PostgreSqlStorageOperation"/> for the entity, or null if the state is not supported.</returns>
	/// <exception cref="ArgumentException">Thrown when the entity does not implement <see cref="IEntity"/>.</exception>
	/// <exception cref="NotSupportedException">Thrown when the entity state is not Add, Update, or Delete.</exception>
	public static async Task<PostgreSqlStorageOperation?> Build(TEntity entity, CancellationToken cancel)
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

	/// <summary>
	/// Builds storage operations for a collection of entities.
	/// </summary>
	/// <param name="entities">The collection of entities to build operations for.</param>
	/// <param name="cancel">Cancellation token for the operation.</param>
	/// <returns>A list of <see cref="PostgreSqlStorageOperation"/> instances for all entities.</returns>
	/// <remarks>
	/// This method processes each entity in the collection sequentially, building appropriate
	/// operations based on each entity's state. Operations are only added to the result list
	/// if they are successfully generated (non-null).
	/// </remarks>
	public static async Task<List<PostgreSqlStorageOperation>> Build(ImmutableArray<TEntity> entities, CancellationToken cancel)
	{
		var result = new List<PostgreSqlStorageOperation>();

		foreach (var entity in entities)
		{
			var operation = await AggregatedCommandBuilder<TEntity>.Build(entity, cancel);

			if (operation is not null)
				result.Add(operation);
		}

		return result;
	}

	/// <summary>
	/// Builds an INSERT operation for an entity.
	/// </summary>
	/// <param name="entity">The entity to insert.</param>
	/// <param name="cancel">Cancellation token for the operation.</param>
	/// <returns>A <see cref="PostgreSqlStorageOperation"/> for the INSERT command.</returns>
	private static Task<PostgreSqlStorageOperation?> BuildInsert(IEntity entity, CancellationToken cancel)
	{
		return new InsertCommandBuilder().Build(entity, cancel);
	}

	/// <summary>
	/// Builds an UPDATE operation for an entity.
	/// </summary>
	/// <param name="entity">The entity to update.</param>
	/// <param name="cancel">Cancellation token for the operation.</param>
	/// <returns>A <see cref="PostgreSqlStorageOperation"/> for the UPDATE command.</returns>
	private static Task<PostgreSqlStorageOperation?> BuildUpdate(IEntity entity, CancellationToken cancel)
	{
		return new UpdateCommandBuilder().Build(entity, cancel);
	}

	/// <summary>
	/// Builds a DELETE operation for an entity.
	/// </summary>
	/// <param name="entity">The entity to delete.</param>
	/// <param name="cancel">Cancellation token for the operation.</param>
	/// <returns>A <see cref="PostgreSqlStorageOperation"/> for the DELETE command.</returns>
	private static Task<PostgreSqlStorageOperation?> BuildDelete(IEntity entity, CancellationToken cancel)
	{
		return new DeleteCommandBuilder().Build(entity, cancel);
	}
}
