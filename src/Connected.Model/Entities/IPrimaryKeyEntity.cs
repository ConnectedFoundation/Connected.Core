namespace Connected.Entities;

/// <summary>
/// Represents an entity that exposes a strongly-typed primary key.
/// </summary>
/// <typeparam name="T">The type of the primary key. Must be non-nullable.</typeparam>
/// <remarks>
/// Types that implement <see cref="IPrimaryKeyEntity{T}"/> provide a stable
/// identifier through the <see cref="Id"/> property. The interface also
/// inherits from <see cref="IEntity"/>, so implementers participate in the
/// runtime change-tracking model via the <see cref="IEntity.State"/> property.
/// </remarks>
public interface IPrimaryKeyEntity<T> : IEntity
	where T : notnull
{
	/// <summary>
	/// Gets the entity's primary key value.
	/// </summary>
	/// <remarks>
	/// The meaning and format of the primary key depend on the domain model and
	/// storage provider. Consumers should treat the <see cref="Id"/> property as
	/// the canonical identifier for the entity and avoid relying on other fields
	/// for uniqueness.
	/// </remarks>
	T Id { get; init; }
}
