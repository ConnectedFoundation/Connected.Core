using Connected.Annotations.Entities;
using Connected.Net.Routing;

namespace Connected.Net;

/// <summary>
/// Provides metadata constants for networking entities and components.
/// </summary>
/// <remarks>
/// This static class centralizes metadata key definitions for network-related entities
/// such as routes, messaging components, and service endpoints. These keys follow a
/// consistent naming convention based on the schema and entity type, ensuring uniform
/// metadata access patterns across the networking subsystem. The metadata keys are
/// used for entity identification, configuration lookup, and runtime component discovery.
/// </remarks>
public static class NetMetaData
{
	/// <summary>
	/// Gets the metadata key for client-side route entities.
	/// </summary>
	/// <value>
	/// A string containing the fully qualified metadata key in the format "schema.entityType.Client".
	/// </value>
	/// <remarks>
	/// Client and server route caches store distinct, incompatible entity types
	/// (<c>Connected.Net.Routing.Client.Route</c> vs <c>Connected.Net.Routing.Server.Route</c>) and
	/// must never share a cache key - a shared key would make both caches read and write the same
	/// underlying bucket, causing <see cref="InvalidCastException"/>s when one side reads entries
	/// written by the other.
	/// </remarks>
	public const string ClientRouteKey = $"{SchemaAttribute.CoreSchema}.{nameof(IRoute)}.Client";

	/// <summary>
	/// Gets the metadata key for server-side route entities.
	/// </summary>
	/// <value>
	/// A string containing the fully qualified metadata key in the format "schema.entityType.Server".
	/// </value>
	/// <remarks>
	/// Client and server route caches store distinct, incompatible entity types
	/// (<c>Connected.Net.Routing.Client.Route</c> vs <c>Connected.Net.Routing.Server.Route</c>) and
	/// must never share a cache key - a shared key would make both caches read and write the same
	/// underlying bucket, causing <see cref="InvalidCastException"/>s when one side reads entries
	/// written by the other.
	/// </remarks>
	public const string ServerRouteKey = $"{SchemaAttribute.CoreSchema}.{nameof(IRoute)}.Server";
}
