using Connected.Annotations.Entities;
using System.Text.Json.Serialization;

namespace Connected.Entities.Concurrency;

public abstract record ConcurrentEntity<TPrimaryKey> : ConsistentEntity<TPrimaryKey>, IConcurrentEntity<TPrimaryKey>
	 where TPrimaryKey : notnull
{
	private int _sync = 0;

	[Persistence(PersistenceMode.InMemory)]
	[JsonIgnore]
	public int Sync
	{
		get => _sync;
		set => Interlocked.Exchange(ref _sync, value);
	}
}
