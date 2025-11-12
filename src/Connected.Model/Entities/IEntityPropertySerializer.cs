namespace Connected.Entities;

/// <summary>
/// Provides custom serialization and deserialization for individual entity properties.
/// </summary>
/// <remarks>
/// Implementers can be used by storage providers or transport layers to transform
/// property values before persistence or when materializing entities. Typical
/// uses include encryption, compression, or conversion to storage-friendly
/// formats. Implementations should be thread-safe if registered as singletons.
/// </remarks>
public interface IEntityPropertySerializer
{
	/// <summary>
	/// Serializes a property value for storage or transport.
	/// </summary>
	/// <param name="entity">The entity instance that contains the property being serialized. Implementations
	/// can use metadata from the entity to influence serialization.</param>
	/// <param name="value">The current property value to serialize. May be <c>null</c>.</param>
	/// <param name="cancel">A cancellation token that can be used to cancel the operation.</param>
	/// <returns>A task that resolves to the serialized representation of the value. The returned
	/// object should be representable by the underlying storage provider (for example a string,
	/// number or binary blob). The returned value may be <c>null</c> if the input was null or if
	/// the serializer chooses to omit the value.</returns>
	Task<object?> Serialize(IEntity entity, object? value, CancellationToken cancel = default);

	/// <summary>
	/// Deserializes a previously serialized property value back into a CLR value suitable for the entity.
	/// </summary>
	/// <param name="entity">The entity instance that will receive the deserialized property value. Implementations
	/// can use metadata from the entity to influence deserialization.</param>
	/// <param name="value">The serialized representation produced by <see cref="Serialize"/>. May be <c>null</c>.</param>
	/// <param name="cancel">A cancellation token that can be used to cancel the operation.</param>
	/// <returns>A task that resolves to the deserialized CLR value (or <c>null</c> if the input was null).</returns>
	Task<object?> Deserialize(IEntity entity, object? value, CancellationToken cancel = default);
}