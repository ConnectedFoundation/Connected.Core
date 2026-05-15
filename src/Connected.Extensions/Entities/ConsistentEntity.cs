using Connected.Annotations;
using Connected.Annotations.Entities;
using System.Text.Json.Serialization;

namespace Connected.Entities;

public abstract record ConsistentEntity<TPrimaryKey> : Entity<TPrimaryKey>, IConsistentEntity<TPrimaryKey>
	 where TPrimaryKey : notnull
{
	[Ordinal(10000), ETag, JsonIgnore, Persistence(PersistenceMode.Read), ReturnValue]
	public string? ETag { get; init; }
}
