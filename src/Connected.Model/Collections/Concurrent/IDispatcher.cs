namespace Connected.Collections.Concurrent;

public interface IDispatcher<TArgs, TJob> : IDisposable
	where TJob : IDispatcherJob<TArgs>
{
	bool Dequeue(out TArgs? item);
	Task<bool> Enqueue(TArgs item);

	CancellationToken CancellationToken { get; }
	void Cancel();
}
