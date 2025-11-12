namespace Connected.Entities;
/// <summary>
/// Represents an entity that supports a simple tag collection stored as a string.
/// </summary>
/// <typeparam name="TPrimaryKey">The type of the entity primary key.</typeparam>
/// <remarks>
/// The <see cref="Tags"/> property is intended to store a lightweight, serialized
/// set of tags associated with the entity (for example a comma-separated list).
/// Storage providers and application code are responsible for choosing and
/// documenting the exact format. This interface inherits from
/// <see cref="IEntity{TPrimaryKey}"/>, so it also participates in change-tracking
/// via the <see cref="IEntity.State"/> property and exposes a strongly-typed
/// primary key through <see cref="IPrimaryKeyEntity{TPrimaryKey}.Id"/>.
/// </remarks>
public interface ITaggedEntity<TPrimaryKey> : IEntity<TPrimaryKey>
	where TPrimaryKey : notnull
{
	/// <summary>
	/// Gets a serialized representation of tags associated with the entity.
	/// </summary>
	/// <remarks>
	/// The format is intentionally unspecified by the interface to allow
	/// application-specific conventions (for example CSV, JSON array, or
	/// other delimiter-separated values). Implementations should document the
	/// expected format and consumer code should parse the value accordingly.
	/// The property may be <c>null</c> when no tags are present.
	/// </remarks>
	string? Tags { get; init; }
}
