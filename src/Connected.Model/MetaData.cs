using Connected.Annotations.Entities;
using Connected.Collections.Queues;

namespace Connected;

/// <summary>
/// Provides metadata constants for core system entities and components.
/// </summary>
/// <remarks>
/// This static class centralizes metadata key definitions used throughout the application
/// for identifying and accessing various system components. These keys follow a consistent
/// naming convention based on the schema and entity type, ensuring uniform metadata access
/// patterns across the platform.
/// </remarks>
public static class MetaData
{
	/// <summary>
	/// Gets the metadata key for queue message entities.
	/// </summary>
	/// <value>
	/// A string containing the fully qualified metadata key in the format "schema.entityType".
	/// </value>
	/// <remarks>
	/// This key is constructed using the core schema prefix combined with the queue message
	/// interface name, providing a unique identifier for queue message metadata operations.
	/// The key follows the pattern: "{CoreSchema}.{IQueueMessage}".
	/// </remarks>
	public const string QueueMessageKey = $"{SchemaAttribute.CoreSchema}.{nameof(IQueueMessage)}";
}
