using System.Collections.Immutable;

namespace Connected.Net.Messaging;

/// <summary>
/// Provides client connection management operations.
/// </summary>
/// <remarks>
/// This interface defines the contract for managing connected client information in a
/// messaging system. It supports adding or updating client registrations, cleaning up
/// inactive clients, removing specific connections, and querying client information.
/// The client registry maintains the state of all active connections, enabling message
/// routing, connection tracking, and identity-based client lookup. This forms the
/// foundation for features such as presence detection, connection lifecycle management,
/// and multi-device support where a single identity can have multiple active connections.
/// </remarks>
public interface IClients
{
	/// <summary>
	/// Adds a new client or updates an existing client registration.
	/// </summary>
	/// <param name="client">The client information to add or update.</param>
	/// <remarks>
	/// This method registers a client connection or updates its information if it already
	/// exists. It is typically called when a new connection is established or when client
	/// properties such as retention deadline need to be updated. The upsert semantics ensure
	/// that client information is always current.
	/// </remarks>
	void AddOrUpdate(IClient client);

	/// <summary>
	/// Cleans up expired or inactive client registrations.
	/// </summary>
	/// <remarks>
	/// This method performs maintenance by removing client registrations that have exceeded
	/// their retention deadline or are no longer active. It should be called periodically
	/// to prevent resource leaks and maintain accurate connection state. This helps ensure
	/// that the client registry reflects the actual set of connected clients.
	/// </remarks>
	void Clean();

	/// <summary>
	/// Removes a client registration by connection identifier.
	/// </summary>
	/// <param name="connectionId">The connection identifier of the client to remove.</param>
	/// <remarks>
	/// This method removes a client from the registry, typically called when a connection
	/// is terminated. It ensures that disconnected clients are promptly removed from the
	/// active client list, preventing messages from being routed to non-existent connections.
	/// </remarks>
	void Remove(string connectionId);

	/// <summary>
	/// Queries all registered clients.
	/// </summary>
	/// <returns>
	/// An immutable list containing all currently registered client information.
	/// </returns>
	/// <remarks>
	/// This method returns a snapshot of all active client connections. The immutable list
	/// ensures that the returned collection remains consistent even if clients are added or
	/// removed concurrently. This is useful for broadcasting operations or administrative
	/// tasks that need visibility into all connected clients.
	/// </remarks>
	IImmutableList<IClient> Query();

	/// <summary>
	/// Selects a client by unique identifier.
	/// </summary>
	/// <param name="id">The unique client identifier.</param>
	/// <returns>
	/// The client information if found, or null if no client with the specified identifier exists.
	/// </returns>
	/// <remarks>
	/// This method retrieves client information by its unique GUID identifier, enabling
	/// client-specific operations based on the client ID rather than connection string.
	/// </remarks>
	IClient? Select(Guid id);

	/// <summary>
	/// Selects a client by connection identifier.
	/// </summary>
	/// <param name="connection">The connection identifier.</param>
	/// <returns>
	/// The client information if found, or null if no client with the specified connection exists.
	/// </returns>
	/// <remarks>
	/// This method retrieves client information by its connection identifier, which is
	/// typically provided by the underlying transport layer. This is the most common way
	/// to look up client information when handling connection-specific events or messages.
	/// </remarks>
	IClient? Select(string connection);
}
