using Connected.Entities;
using Connected.Services;
using System.Collections;
using System.Collections.Immutable;
using System.Data;

namespace Connected.Storage;

/// <summary>
/// Defines the read and write operations on the supported storage providers.
/// </summary>
/// <typeparam name="TEntity">The type of the entity on which operations are performed.</typeparam>
/// <remarks>
/// This interface provides a unified abstraction over different storage providers (SQL Server, PostgreSQL, etc.)
/// and combines LINQ queryable capabilities with entity update operations. It supports:
/// - LINQ queries through IQueryable implementation for flexible data retrieval
/// - Multiple Update overloads with varying levels of control over the update process
/// - Concurrency conflict handling with retry logic through callback functions
/// - DTO-based updates with automatic entity merging
/// - Selective property updates for optimized database operations
/// - Direct storage context operations for advanced scenarios (Execute, Query, Select)
/// - Multiple data reader support for batch operations
/// 
/// The interface enforces that TEntity implements IEntity, ensuring all stored types
/// have the required entity infrastructure for tracking, identity, and state management.
/// 
/// Update operations are designed to handle common scenarios:
/// - Simple entity updates using the basic Update method
/// - DTO-driven updates with concurrency retry support
/// - Custom merge logic for complex entity composition
/// - Fine-grained control over which properties are persisted
/// 
/// Storage context operations (OpenReaders, Execute, Query, Select) provide low-level
/// access when LINQ queries are insufficient or when direct command execution is needed.
/// </remarks>
public interface IStorage<TEntity>
	: IQueryable<TEntity>, IQueryable, IEnumerable<TEntity>, IEnumerable,
		IOrderedQueryable<TEntity>, IOrderedQueryable, IStorageVariableProvider
	where TEntity : IEntity
{
	/// <summary>
	/// Updates the entity against the underlying storage.
	/// </summary>
	/// <param name="entity">The entity to be updated.</param>
	/// <returns>A task that represents the asynchronous operation. The task result contains the updated entity, or null if the update failed.</returns>
	Task<TEntity?> Update(TEntity? entity);

	/// <summary>
	/// Updates the entity against the underlying storage using a DTO with concurrency retry support.
	/// </summary>
	/// <typeparam name="TDto">The type of the DTO containing update data.</typeparam>
	/// <param name="entity">The entity to be updated.</param>
	/// <param name="dto">The DTO containing the update data.</param>
	/// <param name="concurrencyRetrying">The callback function to be called when a concurrency conflict occurs, allowing for retry logic.</param>
	/// <param name="caller">The caller context containing information about the user or system performing the update.</param>
	/// <returns>A task that represents the asynchronous operation. The task result contains the updated entity, or null if the update failed.</returns>
	Task<TEntity?> Update<TDto>(TEntity? entity, TDto dto, Func<Task<TEntity?>>? concurrencyRetrying, ICallerContext caller)
		where TDto : IDto;

	/// <summary>
	/// Updates the entity against the underlying storage using custom merge logic and a concurrency retry support.
	/// </summary>
	/// <param name="entity">The entity to be updated.</param>
	/// <param name="merging">The callback function to merge additional data or apply custom logic before updating the entity.</param>
	/// <param name="concurrencyRetrying">The callback function to be called when a concurrency conflict occurs, allowing for retry logic.</param>
	/// <param name="caller">The caller context containing information about the user or system performing the update.</param>
	/// <returns>A task that represents the asynchronous operation. The task result contains the updated entity, or null if the update failed.</returns>
	Task<TEntity?> Update(TEntity? entity, Func<TEntity, Task<TEntity>> merging, Func<Task<TEntity?>>? concurrencyRetrying, ICallerContext caller);
	/// <summary>
	/// Updates the entity against the underlying storage using a custom merge logic and full configuration including concurrency retry, custom merge logic, and selective property updates.
	/// </summary>
	/// <param name="entity">The entity to be updated.</param>
	/// <param name="merging">The callback function to merge additional data or apply custom logic before updating the entity.</param>
	/// <param name="concurrencyRetrying">The callback function to be called when a concurrency conflict occurs, allowing for retry logic.</param>
	/// <param name="caller">The caller context containing information about the user or system performing the update.</param>
	/// <param name="updatingProperties">A collection specifying which properties should be updated with their new values.</param>
	/// <returns>A task that represents the asynchronous operation. The task result contains the updated entity, or null if the update failed.</returns>
	Task<TEntity?> Update(TEntity? entity, Func<TEntity, Task<TEntity>> merging, Func<Task<TEntity?>>? concurrencyRetrying, ICallerContext caller,
		IEnumerable<string>? updatingProperties);

	/// <summary>
	/// Opens multiple data readers for the specified storage context.
	/// </summary>
	/// <param name="dto">The storage context DTO containing query configuration and parameters.</param>
	/// <returns>A task that represents the asynchronous operation. The task result contains an immutable list of data readers.</returns>
	/// <remarks>
	/// This method is used for batch operations where multiple result sets need to be processed.
	/// Each data reader in the list represents a separate result set from the executed query.
	/// </remarks>
	Task<IImmutableList<IDataReader>> OpenReaders(IStorageContextDto dto);

	/// <summary>
	/// Executes a command against the underlying storage.
	/// </summary>
	/// <param name="dto">The storage context DTO containing command configuration and parameters.</param>
	/// <returns>A task that represents the asynchronous operation. The task result contains the number of rows affected.</returns>
	/// <remarks>
	/// Use this method for non-query operations such as INSERT, UPDATE, DELETE, or stored procedure calls
	/// that modify data and return the number of affected rows.
	/// </remarks>
	Task<int> Execute(IStorageContextDto dto);

	/// <summary>
	/// Queries entities from the underlying storage using the specified context.
	/// </summary>
	/// <param name="dto">The storage context DTO containing query configuration and parameters.</param>
	/// <returns>A task that represents the asynchronous operation. The task result contains an enumerable collection of entities matching the query.</returns>
	/// <remarks>
	/// Use this method when you need direct query execution that bypasses LINQ and works directly
	/// with storage context configuration. Useful for complex queries or stored procedure calls.
	/// </remarks>
	Task<IEnumerable<TEntity>> Query(IStorageContextDto dto);

	/// <summary>
	/// Selects a single entity from the underlying storage using the specified context.
	/// </summary>
	/// <param name="dto">The storage context DTO containing query configuration and parameters.</param>
	/// <returns>A task that represents the asynchronous operation. The task result contains the selected entity, or null if not found.</returns>
	/// <remarks>
	/// Use this method to retrieve a single entity when you need direct query execution
	/// or when the query is too complex for LINQ. Returns null if no entity matches the query.
	/// </remarks>
	Task<TEntity?> Select(IStorageContextDto dto);
}
