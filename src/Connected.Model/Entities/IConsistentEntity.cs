namespace Connected.Entities;

/// <summary>
/// Represents an entity that supports optimistic concurrency using an entity tag (ETag).
/// </summary>
/// <typeparam name="T">The type of the entity primary key.</typeparam>
/// <remarks>
/// Implementations expose the <see cref="ETag"/> property which can be used by storage
/// providers and HTTP APIs to perform conditional updates and detect conflicting changes.
/// This interface extends <see cref="IEntity{T}"/>, and therefore includes the base
/// change-tracking <see cref="IEntity.State"/> and the primary key from
/// <see cref="IPrimaryKeyEntity{T}"/>.
/// </remarks>
public interface IConsistentEntity<T> : IEntity<T>
	 where T : notnull
{
	/// <summary>
	/// Gets the entity tag used for optimistic concurrency checks.
	/// </summary>
	/// <remarks>
	/// The value is typically a server-assigned opaque string (for example a timestamp,
	/// version token, or hash). Storage providers should update this value when the
	/// persisted representation changes. Consumers can send the ETag back to the server
	/// to assert that they are updating the expected version of the entity.
	/// </remarks>
	string? ETag { get; }
}
