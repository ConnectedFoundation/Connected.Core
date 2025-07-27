using Connected.Annotations;
using Connected.Annotations.Entities;
using System.Text.Json.Serialization;

namespace Connected.Entities;

public abstract record ConsistentEntity<TPrimaryKey> : Entity<TPrimaryKey>
	 where TPrimaryKey : notnull
{
	[Ordinal(10000), ETag, JsonIgnore, Persistence(PersistenceMode.Read)]
	public string? ETag { get; init; }
}
