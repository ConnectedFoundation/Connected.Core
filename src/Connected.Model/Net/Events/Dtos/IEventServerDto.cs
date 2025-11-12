using Connected.Services;

namespace Connected.Net.Events.Dtos;

/// <summary>
/// Represents a data transfer object for event server operations.
/// </summary>
/// <remarks>
/// This interface provides the base structure for DTOs used in event server communication.
/// Event servers manage real-time event distribution across connected clients, enabling
/// publish-subscribe patterns and event-driven architectures. The connection identifier
/// associates operations with specific client connections, allowing the event server to
/// route events and manage subscriptions on a per-connection basis. This is fundamental
/// for implementing features like real-time notifications, live updates, and distributed
/// event handling.
/// </remarks>
public interface IEventServerDto
	: IDto
{
	/// <summary>
	/// Gets or sets the connection identifier for the client.
	/// </summary>
	/// <value>
	/// A string representing the unique connection identifier.
	/// </value>
	/// <remarks>
	/// The connection identifier uniquely identifies the client connection involved in
	/// the event server operation, enabling connection-specific event routing and
	/// subscription management.
	/// </remarks>
	string Connection { get; set; }
}
