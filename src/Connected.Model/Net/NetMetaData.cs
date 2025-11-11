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
	/// Gets the metadata key for route entities.
	/// </summary>
	/// <value>
	/// A string containing the fully qualified metadata key in the format "schema.entityType".
	/// </value>
	/// <remarks>
	/// This key is constructed using the core schema prefix combined with the route
	/// interface name, providing a unique identifier for route metadata operations.
	/// The key follows the pattern: "{CoreSchema}.{IRoute}" and is used for route
	/// entity identification, configuration, and persistence operations.
	/// </remarks>
	public const string RouteKey = $"{SchemaAttribute.CoreSchema}.{nameof(IRoute)}";
}
