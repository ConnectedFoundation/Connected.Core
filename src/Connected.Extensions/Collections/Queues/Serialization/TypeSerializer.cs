using Connected.Entities;

namespace Connected.Collections.Queues.Serialization;
/// <summary>
/// Provides custom serialization for queue message Action properties to string format for database storage.
/// </summary>
/// <remarks>
/// TypeSerializer implements the EntityPropertySerializer pattern to handle serialization of System.Type instances
/// to assembly-qualified type name strings. This enables storage of action type references in queue messages
/// for runtime resolution during dequeue operations.
/// The serializer:
/// - Serializes Type instances to "FullTypeName, AssemblyName" format during entity persistence
/// - Deserializes strings back to Type instances using Type.GetType at runtime
/// - Returns null for null values or resolution failures
/// This approach allows queue messages to reference action types that may be deployed or updated independently
/// of the queue storage schema.
/// </remarks>
internal sealed class TypeSerializer : EntityPropertySerializer
{
	/// <summary>
	/// Serializes a Type instance to an assembly-qualified type name string for storage.
	/// </summary>
	/// <param name="value">The Type instance to serialize.</param>
	/// <returns>An assembly-qualified type name string; or null if the value is null or not a Type.</returns>
	/// <remarks>
	/// This method is called by the ORM when persisting queue message entities to the database.
	/// It creates a compact type name format including the full type name and assembly name,
	/// which is sufficient for Type.GetType resolution without culture or version information.
	/// </remarks>
	protected override async Task<object?> OnSerialize(object? value)
	{
		/*
		 * Return null for null values.
		 */
		if (value is null)
			return null;

		/*
		 * Validate the value is a Type instance.
		 */
		if (value is not Type type)
			return null;

		/*
		 * Create an assembly-qualified type name in the format "FullTypeName, AssemblyName".
		 * This format is recognized by Type.GetType for runtime type resolution.
		 */
		return await Task.FromResult<object?>($"{type.FullName}, {type.Assembly.GetName().Name}");
	}

	/// <summary>
	/// Deserializes an assembly-qualified type name string back to a Type instance.
	/// </summary>
	/// <param name="value">The type name string to deserialize.</param>
	/// <returns>The resolved Type instance; or null if resolution fails.</returns>
	/// <remarks>
	/// This method is called by the ORM when loading queue message entities from the database.
	/// It uses Type.GetType to resolve the action type from the assembly-qualified name.
	/// Resolution failures return null rather than throwing, allowing graceful handling of
	/// messages with obsolete or unavailable action types.
	/// </remarks>
	protected override async Task<object?> OnDeserialize(object? value)
	{
		/*
		 * Return null for null values.
		 */
		if (value is null)
			return null;

		/*
		 * Validate the value is a string.
		 */
		if (value is not string s)
			return null;

		/*
		 * Resolve the Type instance from the assembly-qualified name string.
		 * Returns null if the type cannot be found in the loaded assemblies.
		 */
		return await Task.FromResult<object?>(Type.GetType(s));
	}
}