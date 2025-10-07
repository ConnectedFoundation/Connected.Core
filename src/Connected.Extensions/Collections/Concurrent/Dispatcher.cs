using Connected.Authentication;
using Connected.Data;
using Connected.Services;
using Microsoft.Extensions.DependencyInjection;
using System.Collections.Concurrent;

namespace Connected.Collections.Concurrent;

public abstract class Dispatcher<TDto, TJob> : IDispatcher<TDto, TJob>
	where TJob : class, IDispatcherJob<TDto>
{
	private CancellationTokenSource? _tokenSource;

	protected Dispatcher()
	{
		WorkerSize = 1;

		_tokenSource = new();

		Queue = new();
	}

	protected ConcurrentQueue<TDto> Queue { get; }
	public int WorkerSize { get; set; }
	protected bool IsDisposed { get; private set; }
	public CancellationToken CancellationToken
	{
		get
		{
			if (_tokenSource is null)
				throw new NullReferenceException("TokenSource is null");

			return _tokenSource.Token;
		}
	}

	public int Available => Math.Max(0, WorkerSize - Queue.Count);
	public void Cancel()
	{
		_tokenSource?.Cancel();
	}

	public bool Dequeue(out TDto? item)
	{
		return Queue.TryDequeue(out item);
	}

	public bool Enqueue(TDto item)
	{
		Queue.Enqueue(item);

		RunJob();

		return true;
	}

	private void RunJob()
	{
		if (Queue.IsEmpty)
			return;

		if (!Queue.TryDequeue(out TDto? item) || item is null)
			return;

		if (item is IPopReceipt pr && pr.NextVisible <= DateTimeOffset.UtcNow)
			return;
		/*
		 * Dispatcher jobs should be transient so it's safe to request a service from the root collection.
		 */
		var scope = Scope.Create().WithSystemIdentity();

		if (scope.ServiceProvider.GetService<TJob>() is not DispatcherJob<TDto> job)
			throw new NullReferenceException($"{Strings.ErrCreateService} ({typeof(DispatcherJob<TDto>).Name})");

		job.Scope = scope;

		Task.Run(async () =>
		{
			await job.Invoke(item, CancellationToken);
			await job.Scope.Commit();

			await scope.DisposeAsync();
			await job.Scope.Value.DisposeAsync();
			job.Dispose();

			if (!Queue.IsEmpty)
				RunJob();

		}, CancellationToken);
	}

	private void Dispose(bool disposing)
	{
		if (!IsDisposed)
		{
			if (disposing)
			{
				if (_tokenSource is not null)
				{
					if (!_tokenSource.IsCancellationRequested)
						_tokenSource.Cancel();

					_tokenSource.Dispose();
					_tokenSource = null;
				}

				if (Queue is not null)
					Queue.Clear();
			}

			IsDisposed = true;
		}
	}

	protected virtual void OnDisposing()
	{

	}

	public void Dispose()
	{
		Dispose(true);
		GC.SuppressFinalize(this);
	}
}