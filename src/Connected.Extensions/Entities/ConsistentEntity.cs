using Connected.Annotations;
using Connected.Annotations.Entities;
using System.Text.Json.Serialization;

namespace Connected.Entities;

public abstract record ConsistentEntity<TPrimaryKey> : Entity<TPrimaryKey>, IConsistentEntity<TPrimaryKey>
	 where TPrimaryKey : notnull
{
	/// <summary>
	/// The version of the row, kept by the database. An update names it in its WHERE, so a change made by someone else since the entity
	/// was read is refused with a <c>DBConcurrencyException</c> (RA-453).
	/// </summary>
	/// <remarks>
	/// The providers do not return the version a write produced. An <see cref="EntityTopic{TEntity, TEntityImplementation, TPrimaryKey}"/>
	/// reads the entity again for every operation and re-reads it when the write is refused; code that writes through the storage itself
	/// must do the same (the callbacks of <c>EntityStorage.Update</c>) and must not write one entity twice without reading it between.
	/// </remarks>
	[Ordinal(10000), ETag, JsonIgnore, Persistence(PersistenceMode.Read), ReturnValue]
	public string? ETag { get; init; }
}
