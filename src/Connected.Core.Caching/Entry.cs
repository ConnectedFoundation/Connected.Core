using System;

namespace Connected.Caching;

internal class Entry : IEntry
{
	public Entry(string id, object? instance, TimeSpan duration, bool slidingExpiration)
	{
		Id = id;
		Instance = instance;
		SlidingExpiration = slidingExpiration;
		Duration = duration;

		if (Duration > TimeSpan.Zero)
			ExpirationDate = DateTime.UtcNow.AddTicks(duration.Ticks);
	}

	public DateTime ExpirationDate { get; set; }
	public bool SlidingExpiration { get; }
	public TimeSpan Duration { get; set; }
	public object? Instance { get; }
	public string Id { get; }
	public bool Expired => ExpirationDate != DateTime.MinValue && ExpirationDate < DateTime.UtcNow;

	public void Dispose()
	{
		if (Instance is IDisposable disposable)
			disposable.Dispose();
	}
}