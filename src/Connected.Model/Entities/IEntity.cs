using System.Text.Json.Serialization;

namespace Connected.Entities;
/// <summary>
/// Represents the persistence state of an entity instance.
/// </summary>
/// <remarks>
/// This enum is used by the runtime to track whether an entity should be
/// inserted, updated or removed from persistent storage.
/// </remarks>
public enum State : byte
{
	/// <summary>
	/// New entity that should be inserted into storage.
	/// </summary>
	Add = 0,

	/// <summary>
	/// Existing entity that has been modified and should be updated in storage.
	/// </summary>
	Update = 1,

	/// <summary>
	/// Entity that should be deleted from storage.
	/// </summary>
	Delete = 2
}
/// <summary>
/// Represents a domain entity managed by the runtime.
/// </summary>
/// <remarks>
/// Implementing types are expected to be plain data holders that expose an
/// <see cref="State"/> value used for change-tracking during persistence
/// operations. The <see cref="State"/> property is annotated with
/// <see cref="JsonIgnoreAttribute"/> to exclude it from JSON payloads.
/// </remarks>
public interface IEntity
{
	/// <summary>
	/// Gets the change-tracking state of the entity.
	/// </summary>
	[JsonIgnore]
	State State { get; init; }
}
/// <summary>
/// Represents an entity that exposes a strongly-typed primary key.
/// </summary>
/// <typeparam name="T">The type of the primary key. Must be non-nullable.</typeparam>
/// <remarks>
/// This interface combines the base <see cref="IEntity"/> contract with
/// <see cref="IPrimaryKeyEntity{T}"/> (which provides the <c>Id</c> property).
/// </remarks>
public interface IEntity<T> : IEntity, IPrimaryKeyEntity<T>
	where T : notnull
{
}
