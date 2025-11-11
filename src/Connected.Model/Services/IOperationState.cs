using System.Collections.Immutable;

namespace Connected.Services;

/// <summary>
/// Provides state management capabilities for service operations.
/// </summary>
/// <remarks>
/// This interface enables service operations to store and retrieve state during their execution.
/// State can be associated with entities either by type alone or by type and primary key,
/// allowing fine-grained state management across complex operation workflows.
/// </remarks>
public interface IOperationState
{
	/// <summary>
	/// Sets the state for an entity of the specified type.
	/// </summary>
	/// <typeparam name="TEntity">The type of the entity.</typeparam>
	/// <param name="entity">The entity instance to store, or null to clear the state.</param>
	/// <returns>The entity that was stored, or null if the state was cleared.</returns>
	TEntity? SetState<TEntity>(TEntity? entity);

	/// <summary>
	/// Gets the state for an entity of the specified type.
	/// </summary>
	/// <typeparam name="TEntity">The type of the entity.</typeparam>
	/// <returns>The stored entity instance, or null if no state exists.</returns>
	TEntity? GetState<TEntity>();

	/// <summary>
	/// Sets the state for an entity identified by type and primary key.
	/// </summary>
	/// <typeparam name="TEntity">The type of the entity.</typeparam>
	/// <typeparam name="TPrimaryKey">The type of the primary key.</typeparam>
	/// <param name="entity">The entity instance to store, or null to clear the state.</param>
	/// <param name="id">The primary key that identifies the entity.</param>
	/// <returns>The entity that was stored, or null if the state was cleared.</returns>
	TEntity? SetState<TEntity, TPrimaryKey>(TEntity? entity, TPrimaryKey id);

	/// <summary>
	/// Gets the state for an entity identified by type and primary key.
	/// </summary>
	/// <typeparam name="TEntity">The type of the entity.</typeparam>
	/// <typeparam name="TPrimaryKey">The type of the primary key.</typeparam>
	/// <param name="id">The primary key that identifies the entity.</param>
	/// <returns>The stored entity instance, or null if no state exists for the specified key.</returns>
	TEntity? GetState<TEntity, TPrimaryKey>(TPrimaryKey id);

	/// <summary>
	/// Gets all stored states for entities of the specified type.
	/// </summary>
	/// <typeparam name="TEntity">The type of the entities.</typeparam>
	/// <returns>An immutable list containing all stored entity instances of the specified type.</returns>
	IImmutableList<TEntity> GetStates<TEntity>();
}
