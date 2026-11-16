using Connected.Annotations;
using Connected.Annotations.Entities;
using System.Text.Json.Serialization;

namespace Connected.Entities;

public abstract record ConsistentEntity<TPrimaryKey> : Entity<TPrimaryKey>, IConsistentEntity<TPrimaryKey>
	 where TPrimaryKey : notnull
{
	/// <summary>
	/// The version of the row. The provider produces it: the database keeps it for SQL Server, a sequence for PostgreSQL. An update names it in
	/// its WHERE, so a change made by someone else since the entity was read is refused with a <c>DBConcurrencyException</c>, and an insert or
	/// an update hands back the version it wrote, so that the entity the storage returns can be written again.
	/// </summary>
	/// <remarks>
	/// An entity that carries no version (built from a dto, say) is updated without the check.
	/// </remarks>
	[Ordinal(10000), ETag, JsonIgnore, Persistence(PersistenceMode.Read), ReturnValue]
	public string? ETag { get; init; }
}
