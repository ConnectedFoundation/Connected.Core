using Connected.Net.Dtos;
using System.Collections.Immutable;

namespace Connected.Net.Messaging;

/// <summary>
/// Provides message queue management for client connections.
/// </summary>
/// <typeparam name="TMessage">The type of messages stored in the queue.</typeparam>
/// <remarks>
/// This interface defines operations for managing per-client message queues in a messaging
/// system. It supports enqueueing messages for clients, dequeuing messages for delivery,
/// acknowledging received messages, and cleaning up stale messages or connections. The
/// generic type parameter allows different message types to be queued while maintaining
/// type safety. This forms the foundation for reliable message delivery patterns where
/// messages are queued until successfully delivered and acknowledged by clients. The
/// interface supports both connection-specific and system-wide queue management operations.
/// </remarks>
public interface IClientMessages<TMessage>
	where TMessage : IMessage
{
	/// <summary>
	/// Dequeues messages that are ready for delivery.
	/// </summary>
	/// <returns>
	/// An immutable list containing messages that should be delivered to clients.
	/// </returns>
	/// <remarks>
	/// This method retrieves messages from the queue that are currently visible and ready
	/// for delivery. Messages may be temporarily hidden after delivery to implement retry
	/// logic with visibility timeouts. The returned list can be empty if no messages are
	/// currently ready for delivery.
	/// </remarks>
	IImmutableList<TMessage> Dequeue();

	/// <summary>
	/// Cleans up expired messages and inactive connections.
	/// </summary>
	/// <remarks>
	/// This method performs maintenance operations such as removing messages that have
	/// exceeded their retention deadline and cleaning up resources for disconnected clients.
	/// It should be called periodically to prevent resource leaks and maintain optimal
	/// queue performance.
	/// </remarks>
	void Clean();

	/// <summary>
	/// Removes all messages associated with a specific connection.
	/// </summary>
	/// <param name="connectionId">The connection identifier whose messages should be removed.</param>
	/// <remarks>
	/// This method is typically called when a client disconnects to clean up any pending
	/// messages that were queued for delivery. It ensures that resources are properly
	/// released when connections are terminated.
	/// </remarks>
	void Remove(string connectionId);

	/// <summary>
	/// Removes a specific acknowledged message for a connection.
	/// </summary>
	/// <param name="connection">The connection identifier.</param>
	/// <param name="dto">The acknowledgement containing the message identifier to remove.</param>
	/// <remarks>
	/// This method removes a message from the queue after it has been successfully acknowledged
	/// by the client. This confirms that the message was received and processed, allowing it
	/// to be permanently removed from the queue.
	/// </remarks>
	void Remove(string connection, IMessageAcknowledgeDto dto);

	/// <summary>
	/// Removes a specific message identified by key for a connection.
	/// </summary>
	/// <param name="connection">The connection identifier.</param>
	/// <param name="key">The message key identifying which message to remove.</param>
	/// <remarks>
	/// This method removes a message from the queue using its key identifier. This provides
	/// an alternative to ID-based removal when messages are tracked using string keys rather
	/// than numeric identifiers.
	/// </remarks>
	void Remove(string connection, string key);

	/// <summary>
	/// Adds a message to the queue for a specific client.
	/// </summary>
	/// <param name="client">The client identifier (connection or identity) for whom to queue the message.</param>
	/// <param name="message">The message to add to the queue.</param>
	/// <remarks>
	/// This method enqueues a message for delivery to the specified client. The message will
	/// be held in the queue until it is dequeued for delivery and subsequently acknowledged
	/// by the client. The client parameter determines which client should receive the message.
	/// </remarks>
	void Add(string client, TMessage message);
}
