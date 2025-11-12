namespace Connected.Entities;
/// <summary>
/// Represents a container that references another entity by name and identifier.
/// </summary>
/// <typeparam name="TPrimaryKey">The type of the container's primary key.</typeparam>
/// <remarks>
/// This interface extends <see cref="IPrimaryKeyEntity{TPrimaryKey}"/>, and through
/// that relationship includes the base <see cref="IEntity"/> contract which provides
/// change-tracking via the <see cref="State"/> property. Implementations are used
/// to store lightweight references to other entities by their logical name and id.
/// </remarks>
public interface IEntityContainer<TPrimaryKey> : IPrimaryKeyEntity<TPrimaryKey>
	where TPrimaryKey : notnull
{
	/// <summary>
	/// Gets the logical name of the referenced entity type (for example the CLR type
	/// name or a domain-specific entity identifier).
	/// </summary>
	string Entity { get; init; }
	/// <summary>
	/// Gets the identifier of the referenced entity as a string. This value represents
	/// the primary key of the referenced entity and is typically used for lookups or
	/// serialization where a string form is required.
	/// </summary>
	string EntityId { get; init; }
}
