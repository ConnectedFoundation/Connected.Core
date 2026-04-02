using Connected.Entities;
using Connected.Reflection;
using Connected.Services;
using System.Text.Json;

namespace Connected.Collections.Queues.Serialization;
/// <summary>
/// Provides custom serialization for queue message DTO properties to binary format for database storage.
/// </summary>
/// <remarks>
/// DtoSerializer implements the EntityPropertySerializer pattern to handle serialization of IDto instances
/// to byte arrays using System.Text.Json. This enables storage of arbitrary DTO payloads in queue messages
/// without requiring specific column types for each DTO property.
/// The serializer:
/// - Serializes DTOs to UTF-8 encoded JSON bytes during entity persistence
/// - Deserializes bytes back to concrete DTO types using the DtoTypeName property for type resolution
/// - Returns null for null values to support optional DTO properties
/// Type information is stored separately in the DtoTypeName property to enable correct deserialization
/// when the message is dequeued.
/// </remarks>
internal sealed class DtoSerializer : EntityPropertySerializer
{
	/// <summary>
	/// Serializes a DTO instance to UTF-8 encoded JSON bytes for storage.
	/// </summary>
	/// <param name="value">The DTO instance to serialize.</param>
	/// <returns>A byte array containing the serialized DTO; or null if the value is null or not an IDto.</returns>
	/// <remarks>
	/// This method is called by the ORM when persisting queue message entities to the database.
	/// It uses System.Text.Json with default serialization options to convert the DTO to a compact
	/// binary representation suitable for storage in VARBINARY columns.
	/// </remarks>
	protected override async Task<object?> OnSerialize(object? value)
	{
		/*
		 * Return null for null values.
		 */
		if (value is null)
			return null;

		/*
		 * Validate the value is an IDto instance.
		 */
		if (value is not IDto dto)
			return null;

		/*
		 * Serialize the DTO to UTF-8 encoded JSON bytes using the concrete DTO type for proper polymorphism.
		 */
		return await Task.FromResult<object?>(JsonSerializer.SerializeToUtf8Bytes(dto, dto.GetType(), JsonSerializerOptions.Default));
	}

	/// <summary>
	/// Deserializes UTF-8 encoded JSON bytes back to the concrete DTO type.
	/// </summary>
	/// <param name="value">The byte array to deserialize.</param>
	/// <returns>The deserialized DTO instance; or null if deserialization fails.</returns>
	/// <remarks>
	/// This method is called by the ORM when loading queue message entities from the database.
	/// It resolves the concrete DTO type from the entity's DtoTypeName property and deserializes
	/// the byte array using System.Text.Json.
	/// Type resolution failures return null rather than throwing, allowing graceful handling of
	/// messages with obsolete or unavailable DTO types.
	/// </remarks>
	protected override async Task<object?> OnDeserialize(object? value)
	{
		/*
		 * Return null for null values.
		 */
		if (value is null)
			return null;

		/*
		 * Validate the value is a byte array.
		 */
		if (value is not byte[] bytes)
			return null;

		/*
		 * Retrieve the entity context to access the DtoTypeName property for type resolution.
		 */
		if (Entity is not QueueMessage entity || entity.DtoTypeName is null)
			return null;

		/*
		 * Resolve the concrete DTO type from the assembly-qualified type name.
		 */
		var dtoType = Types.GetType(entity.DtoTypeName);

		if (dtoType is null)
			return null;

		/*
		 * Deserialize the JSON bytes to the resolved DTO type.
		 */
		var span = new ReadOnlySpan<byte>(bytes);

		return await Task.FromResult(JsonSerializer.Deserialize(span, dtoType, JsonSerializerOptions.Default));
	}
}