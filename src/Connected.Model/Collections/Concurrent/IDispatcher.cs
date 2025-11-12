namespace Connected.Collections.Concurrent;
/// <summary>
/// Defines a concurrent dispatcher that queues work items and assigns them to jobs capable of processing
/// argument payloads of type <typeparamref name="TArgs"/>.
/// </summary>
/// <typeparam name="TArgs">The argument type enqueued for processing.</typeparam>
/// <typeparam name="TJob">The job type that processes arguments.</typeparam>
/// <remarks>
/// Provides queue-based coordination over dispatcher jobs: items are enqueued asynchronously and dequeued
/// for processing until cancellation is requested. Implementations should ensure thread-safety for queue
/// operations and cooperatively observe the <see cref="CancellationToken"/>.
/// </remarks>
public interface IDispatcher<TArgs, TJob>
	: IDisposable
	where TJob : IDispatcherJob<TArgs>
{
	/// <summary>
	/// Attempts to dequeue the next argument item.
	/// </summary>
	/// <param name="item">The dequeued item if available; otherwise default.</param>
	/// <returns>True if an item was dequeued; false if the queue was empty.</returns>
	bool Dequeue(out TArgs? item);
	/// <summary>
	/// Enqueues a new argument item for processing.
	/// </summary>
	/// <param name="item">The argument item to enqueue.</param>
	/// <returns>True if the item was accepted; false otherwise.</returns>
	Task<bool> Enqueue(TArgs item);
	/// <summary>
	/// Gets the cancellation token used to signal termination to dispatcher operations.
	/// </summary>
	CancellationToken CancellationToken { get; }
	/// <summary>
	/// Requests cancellation of dispatcher processing.
	/// </summary>
	void Cancel();
}
