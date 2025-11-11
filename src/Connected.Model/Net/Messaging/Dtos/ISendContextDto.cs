using Connected.Services;

namespace Connected.Net.Messaging.Dtos;

/// <summary>
/// Defines flags for filtering message recipients.
/// </summary>
/// <remarks>
/// This flags enumeration specifies filtering criteria for determining which clients
/// should receive a message. Multiple flags can be combined using bitwise operations
/// to create complex recipient filtering logic. This enables scenarios such as sending
/// messages to all connections associated with an identity while excluding the sender,
/// or targeting specific connections with precise control over message distribution.
/// </remarks>
[Flags]
public enum SendFilterFlags
{
	/// <summary>
	/// Indicates no filtering is applied; the message is sent to the default recipients.
	/// </summary>
	None = 0,

	/// <summary>
	/// Indicates the message should be sent to all connections associated with the sender's identity.
	/// </summary>
	/// <remarks>
	/// This flag enables broadcasting messages to all connections belonging to the same
	/// user or identity, useful for synchronizing state across multiple devices or sessions.
	/// </remarks>
	IdentityConnections = 1,

	/// <summary>
	/// Indicates the sender's connection should be excluded from receiving the message.
	/// </summary>
	/// <remarks>
	/// This flag is commonly used to prevent echoing messages back to the sender,
	/// such as when broadcasting updates that the sender already knows about.
	/// </remarks>
	ExceptSender = 2
}

/// <summary>
/// Represents a data transfer object containing message sending context and routing information.
/// </summary>
/// <remarks>
/// This interface encapsulates the parameters that control how messages are routed and
/// delivered to clients. It includes the target method, filtering flags, and optional
/// connection and identity identifiers for precise recipient targeting. The context enables
/// flexible message distribution strategies including point-to-point, broadcast, filtered
/// broadcast, and identity-based routing. This is fundamental for implementing real-time
/// messaging patterns in distributed applications where messages need to be delivered to
/// specific clients or groups of clients based on various criteria.
/// </remarks>
public interface ISendContextDto
	: IDto
{
	/// <summary>
	/// Gets or sets the name of the method to invoke on the client.
	/// </summary>
	/// <value>
	/// A string representing the client method name.
	/// </value>
	/// <remarks>
	/// The method name identifies which client-side handler should process the message,
	/// enabling the server to invoke specific client methods remotely. This follows
	/// the remote procedure call pattern common in real-time messaging frameworks.
	/// </remarks>
	string Method { get; set; }

	/// <summary>
	/// Gets or sets the filtering flags that control recipient selection.
	/// </summary>
	/// <value>
	/// A <see cref="SendFilterFlags"/> value specifying how recipients should be filtered.
	/// </value>
	/// <remarks>
	/// The filter flags control which clients receive the message based on criteria such
	/// as identity matching or sender exclusion. Multiple flags can be combined to create
	/// complex filtering logic.
	/// </remarks>
	SendFilterFlags Filter { get; set; }

	/// <summary>
	/// Gets or sets the specific connection identifier to target.
	/// </summary>
	/// <value>
	/// A string representing the connection identifier, or null to not filter by connection.
	/// </value>
	/// <remarks>
	/// When specified, the message is sent only to the client with this connection identifier,
	/// enabling point-to-point message delivery. When null, the Filter property determines
	/// recipient selection.
	/// </remarks>
	string? Connection { get; set; }

	/// <summary>
	/// Gets or sets the identity identifier for filtering recipients.
	/// </summary>
	/// <value>
	/// A string representing the identity identifier, or null to not filter by identity.
	/// </value>
	/// <remarks>
	/// When specified in conjunction with appropriate filter flags, messages can be sent
	/// to all connections associated with this identity, enabling multi-device synchronization
	/// and identity-based message routing.
	/// </remarks>
	string? Identity { get; set; }
}