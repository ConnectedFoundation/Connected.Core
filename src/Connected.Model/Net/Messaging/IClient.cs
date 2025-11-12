namespace Connected.Net.Messaging;

/// <summary>
/// Represents a connected client in the messaging system.
/// </summary>
/// <remarks>
/// This interface encapsulates information about a client connection including its unique
/// identifier, connection string, associated identity, and message retention deadline.
/// Clients are tracked by the messaging infrastructure to enable message routing, delivery
/// tracking, and connection lifecycle management. The retention deadline determines how long
/// messages should be kept for this client before being discarded. The interface implements
/// IComparable to support client ordering and sorting operations, which is useful for
/// managing client collections and implementing priority-based message delivery.
/// </remarks>
public interface IClient
	: IComparable<IClient>
{
	/// <summary>
	/// Gets or sets the unique identifier for this client.
	/// </summary>
	/// <value>
	/// A <see cref="Guid"/> uniquely identifying the client instance.
	/// </value>
	/// <remarks>
	/// The client identifier is unique across all connections and is used for client
	/// tracking, message routing, and correlation purposes throughout the messaging system.
	/// </remarks>
	Guid Id { get; set; }

	/// <summary>
	/// Gets or sets the connection identifier for this client.
	/// </summary>
	/// <value>
	/// A string representing the connection identifier.
	/// </value>
	/// <remarks>
	/// The connection identifier associates the client with a specific network connection
	/// or communication channel. This is typically provided by the underlying transport
	/// mechanism such as SignalR, WebSockets, or other real-time communication frameworks.
	/// </remarks>
	string Connection { get; set; }

	/// <summary>
	/// Gets the identity associated with this client.
	/// </summary>
	/// <value>
	/// A string representing the identity identifier, or null if the client is not authenticated.
	/// </value>
	/// <remarks>
	/// The identity links the client to an authenticated user or principal, enabling
	/// identity-based message routing and authorization. Multiple clients can share the
	/// same identity when a user has multiple active connections or sessions.
	/// </remarks>
	string? Identity { get; }

	/// <summary>
	/// Gets or sets the deadline for retaining messages for this client.
	/// </summary>
	/// <value>
	/// A <see cref="DateTime"/> representing when undelivered messages should be discarded.
	/// </value>
	/// <remarks>
	/// The retention deadline controls how long messages are kept in queues for this client
	/// before being automatically removed. This prevents indefinite message accumulation for
	/// disconnected or slow clients and helps manage memory and storage resources.
	/// </remarks>
	DateTime RetentionDeadline { get; set; }
}
