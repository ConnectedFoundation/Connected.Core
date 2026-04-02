using Connected.Caching;
using System.Collections.Immutable;

namespace Connected.Collections.Queues;
/// <summary>
/// Defines the contract for a specialized cache managing queue message entities.
/// </summary>
/// <remarks>
/// The queue message cache extends the standard entity cache pattern to provide queue-specific query operations.
/// It maintains an in-memory representation of queue messages synchronized with the underlying storage, enabling
/// efficient message selection, filtering, and dequeue operations without repeated database queries.
/// The cache supports:
/// - Selection by pop receipt for tracking in-flight messages during processing
/// - Selection by action type and group for debouncing and duplicate prevention
/// - Bulk query operations for host dequeue operations
/// - Synchronization with storage through refresh and update operations
/// Cache entries are updated whenever messages are enqueued, dequeued, or completed. The cache lifetime is typically
/// scoped to the application lifetime, providing a shared view of the queue state across all hosts and contexts.
/// </remarks>
public interface IQueueMessageCache
	: IEntityCache<IQueueMessage, long>
{
	/// <summary>
	/// Selects a queue message by action type and group identifier.
	/// </summary>
	/// <param name="client">The action type that processes the message.</param>
	/// <param name="batch">The group identifier used for debouncing and duplicate prevention.</param>
	/// <returns>The matching queue message if found; otherwise null.</returns>
	/// <remarks>
	/// This method is used during message validation to detect duplicate messages for the same group.
	/// It enables the debouncing logic that prevents multiple messages with the same group from being active
	/// in the queue simultaneously. The combination of action type and group identifier uniquely identifies
	/// a processing context.
	/// </remarks>
	Task<IQueueMessage?> Select(Type client, string batch);

	/// <summary>
	/// Retrieves all queue messages currently in the cache.
	/// </summary>
	/// <returns>An immutable list containing all cached queue messages.</returns>
	/// <remarks>
	/// This method is called by queue hosts during the dequeue operation to select candidate messages
	/// for processing. The returned collection is filtered and sorted by priority, visibility timestamp,
	/// and insertion order to determine which messages should be dequeued next.
	/// The immutable return type ensures thread safety for concurrent access by multiple hosts.
	/// </remarks>
	Task<IImmutableList<IQueueMessage>> Query();

	/// <summary>
	/// Selects a queue message by its pop receipt identifier.
	/// </summary>
	/// <param name="popReceipt">The unique pop receipt assigned when the message was dequeued.</param>
	/// <returns>The matching queue message if found; otherwise null.</returns>
	/// <remarks>
	/// Pop receipts are generated when a message is dequeued and serve as a unique identifier for tracking
	/// in-flight messages during processing. This method is used by queue jobs to refresh message state,
	/// extend visibility windows, and complete or delete messages after processing.
	/// </remarks>
	Task<IQueueMessage?> Select(Guid popReceipt);

	/// <summary>
	/// Deletes a queue message from the cache by its pop receipt.
	/// </summary>
	/// <param name="popReceipt">The unique pop receipt of the message to delete.</param>
	/// <returns>A task representing the asynchronous delete operation.</returns>
	/// <remarks>
	/// This method removes the message from the cache after successful processing or when the message
	/// is deemed invalid (expired or exceeded max dequeue count). It should be called in conjunction with
	/// the corresponding storage delete operation to maintain consistency.
	/// </remarks>
	Task Delete(Guid popReceipt);

	/// <summary>
	/// Deletes a queue message from the cache by its primary key identifier.
	/// </summary>
	/// <param name="id">The primary key identifier of the message to delete.</param>
	/// <returns>A task representing the asynchronous delete operation.</returns>
	/// <remarks>
	/// This method is used when the pop receipt is not available, such as during cleanup of expired messages
	/// identified during the dequeue scan. It should be called in conjunction with the corresponding storage
	/// delete operation to maintain consistency.
	/// </remarks>
	Task Delete(long id);

	/// <summary>
	/// Selects a queue message by its primary key identifier.
	/// </summary>
	/// <param name="id">The primary key identifier of the message.</param>
	/// <returns>The matching queue message if found; otherwise null.</returns>
	/// <remarks>
	/// This method is used to retrieve messages by their stable identifier, typically during refresh operations
	/// after concurrency conflicts or when resolving message state after storage updates.
	/// </remarks>
	Task<IQueueMessage?> Select(long id);

	/// <summary>
	/// Updates a queue message in the cache with modified state.
	/// </summary>
	/// <param name="message">The message entity containing updated property values.</param>
	/// <returns>A task representing the asynchronous update operation.</returns>
	/// <remarks>
	/// This method synchronizes the cache with storage after message state changes including:
	/// - Visibility timeout extensions during processing
	/// - Dequeue count increments
	/// - Pop receipt assignments
	/// - NextVisible timestamp updates
	/// The cache entry is replaced with the provided message instance, ensuring subsequent queries
	/// reflect the current state.
	/// </remarks>
	Task Update(IQueueMessage message);
}
