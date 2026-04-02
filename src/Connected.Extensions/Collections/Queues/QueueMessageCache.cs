using Connected.Caching;
using Connected.Storage;
using System.Collections.Immutable;

namespace Connected.Collections.Queues;
/// <summary>
/// Provides an abstract base class for implementing queue message caches with specialized query operations.
/// </summary>
/// <typeparam name="TEntity">The concrete queue message entity type.</typeparam>
/// <param name="cache">The caching service for managing cache entries and lifecycle.</param>
/// <param name="storage">The storage provider for retrieving messages from persistence when cache misses occur.</param>
/// <param name="key">The unique cache key identifying this cache instance in the caching service.</param>
/// <remarks>
/// QueueMessageCache extends the generic EntityCache pattern to provide queue-specific operations including:
/// - Selection by action type and group for debouncing and duplicate detection
/// - Selection by pop receipt for tracking in-flight messages during processing
/// - Bulk query operations returning all messages for dequeue candidate selection
/// - Direct update operations for synchronizing message state after enqueue or dequeue
/// The cache maintains a synchronized view of queue messages across all queue contexts and hosts in the application.
/// Cache entries typically have no expiration (TimeSpan.Zero) since they should remain until explicitly deleted.
/// Derived classes must provide a unique cache key to avoid conflicts with other cache instances.
/// </remarks>
public abstract class QueueMessageCache<TEntity>(ICachingService cache, IStorageProvider storage, string key)
	: EntityCache<IQueueMessage, TEntity, long>(cache, storage, key), IQueueMessageCache
	where TEntity : class, IQueueMessage
{
	/// <summary>
	/// Selects a queue message by action type and group identifier for duplicate detection.
	/// </summary>
	/// <param name="client">The action type that processes the message.</param>
	/// <param name="batch">The group identifier used for debouncing.</param>
	/// <returns>The matching queue message if found; otherwise null.</returns>
	/// <inheritdoc/>
	public async Task<IQueueMessage?> Select(Type client, string batch)
	{
		/*
		 * Query the cache for a message matching both the action type and group identifier.
		 * This enables debouncing by detecting duplicate messages for the same processing context.
		 */
		return await Get(f => string.Equals(f.Group, batch, StringComparison.OrdinalIgnoreCase) && f.Action == client);
	}

	/// <summary>
	/// Retrieves all queue messages currently in the cache for dequeue candidate selection.
	/// </summary>
	/// <returns>An immutable list containing all cached queue messages.</returns>
	/// <inheritdoc/>
	public async Task<IImmutableList<IQueueMessage>> Query()
	{
		/*
		 * Return all cached messages as an immutable list for thread-safe iteration by queue hosts.
		 */
		return await All();
	}

	/// <summary>
	/// Selects a queue message by its pop receipt for tracking during processing.
	/// </summary>
	/// <param name="popReceipt">The unique pop receipt assigned when the message was dequeued.</param>
	/// <returns>The matching queue message if found; otherwise null.</returns>
	/// <inheritdoc/>
	public async Task<IQueueMessage?> Select(Guid popReceipt)
	{
		/*
		 * Query by pop receipt to retrieve in-flight messages for visibility extension or completion.
		 */
		return await Get(f => f.PopReceipt == popReceipt);
	}

	/// <summary>
	/// Selects a queue message by its primary key identifier.
	/// </summary>
	/// <param name="id">The primary key identifier of the message.</param>
	/// <returns>The matching queue message if found; otherwise null.</returns>
	/// <inheritdoc/>
	public async Task<IQueueMessage?> Select(long id)
	{
		/*
		 * Query by primary key for cache refresh operations after concurrency conflicts.
		 */
		return await Get(id);
	}

	/// <summary>
	/// Deletes a queue message from the cache by its pop receipt.
	/// </summary>
	/// <param name="popReceipt">The unique pop receipt of the message to delete.</param>
	/// <returns>A task representing the asynchronous delete operation.</returns>
	/// <inheritdoc/>
	public async Task Delete(Guid popReceipt)
	{
		/*
		 * Remove the message from cache using the pop receipt after successful processing or expiration.
		 */
		await Remove(f => f.PopReceipt == popReceipt);
	}

	/// <summary>
	/// Deletes a queue message from the cache by its primary key identifier.
	/// </summary>
	/// <param name="id">The primary key identifier of the message to delete.</param>
	/// <returns>A task representing the asynchronous delete operation.</returns>
	/// <inheritdoc/>
	public async Task Delete(long id)
	{
		/*
		 * Remove the message from cache using the primary key during cleanup operations.
		 */
		await Remove(id);
	}

	/// <summary>
	/// Updates a queue message in the cache with modified state.
	/// </summary>
	/// <param name="message">The message entity containing updated property values.</param>
	/// <returns>A task representing the asynchronous update operation.</returns>
	/// <inheritdoc/>
	public Task Update(IQueueMessage message)
	{
		/*
		 * Replace the cache entry with the updated message state.
		 * Use TimeSpan.Zero to prevent expiration since messages remain until explicitly deleted.
		 */
		Set(message.Id, message, TimeSpan.Zero);

		return Task.CompletedTask;
	}
}