using Connected.Entities;
using Connected.Storage;

namespace Connected.Caching;

internal class Entry : IEntry
{
	public Entry(string id, object? instance, TimeSpan duration, bool slidingExpiration, CacheEntryMergeBehavior merge)
	{
		Id = id;
		Instance = instance;
		SlidingExpiration = slidingExpiration;
		Duration = duration;
		Merge = merge;

		if (Duration > TimeSpan.Zero)
			ExpirationDate = DateTime.UtcNow.AddTicks(duration.Ticks);
	}

	public DateTime ExpirationDate { get; set; }
	public bool SlidingExpiration { get; private set; }
	public TimeSpan Duration { get; set; }
	public CacheEntryMergeBehavior Merge { get; set; }
	public object? Instance { get; private set; }
	public string Id { get; }
	public bool Expired => ExpirationDate != DateTime.MinValue && ExpirationDate < DateTime.UtcNow;

	public void Set(object? instance, TimeSpan duration, bool slidingExpiration, CacheEntryMergeBehavior merge)
	{
		/*
		 * When both the cached and incoming instances are consistent entities, we use
		 * their ETags (which are EntityVersion hex strings backed by a monotonic ulong)
		 * to decide whether the incoming value is newer. If the incoming ETag does not
		 * parse or is not strictly greater than the current one we discard the update to
		 * avoid overwriting a newer version with a stale one.
		 */
		if (Instance is not null && instance is not null
			&& IsConsistentEntity(Instance) && IsConsistentEntity(instance))
		{
			var currentVersion = EntityVersion.Parse(GetETag(Instance));
			var incomingVersion = EntityVersion.Parse(GetETag(instance));

			if (currentVersion is not null && incomingVersion is not null
				&& incomingVersion <= currentVersion)
				return;
		}

		Instance = instance;
		Duration = duration;
		SlidingExpiration = slidingExpiration;
		Merge = merge;

		ExpirationDate = duration > TimeSpan.Zero
			? DateTime.UtcNow.AddTicks(duration.Ticks)
			: DateTime.MinValue;
	}

	private static bool IsConsistentEntity(object instance)
	{
		return instance.GetType().GetInterfaces()
			.Any(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IConsistentEntity<>));
	}

	private static string? GetETag(object instance)
	{
		/*
		 * ETag is declared on IConsistentEntity<T>. We resolve it through the closed
		 * generic interface so we don't need to know the primary key type at compile time.
		 */
		var consistentInterface = instance.GetType().GetInterfaces()
			.FirstOrDefault(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IConsistentEntity<>));

		return consistentInterface?
			.GetProperty(nameof(IConsistentEntity<object>.ETag))?
			.GetValue(instance) as string;
	}

	public void Dispose()
	{
		if (Instance is IDisposable disposable)
			disposable.Dispose();
	}
}