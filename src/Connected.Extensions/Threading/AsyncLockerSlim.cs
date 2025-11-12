using System.Threading;
using System.Threading.Tasks;
using System;

namespace Connected.Threading;

public class AsyncLockerSlim : IDisposable
{
	/*
	 * this should be upgraded in the future:
	 * https://stackoverflow.com/questions/24139084/semaphoreslim-waitasync-before-after-try-block/61806749#61806749
	 */
	private readonly SemaphoreSlim _semaphore = new(1, 1);
	private bool _disposed;

	public async Task LockAsync(Func<Task> worker)
	{
		await _semaphore.WaitAsync();

		try
		{
			await worker();
		}
		finally
		{
			_semaphore.Release();
		}
	}

	public async Task<T> LockAsync<T>(Func<Task<T>> worker)
	{
		await _semaphore.WaitAsync();

		try
		{
			return await worker();
		}
		finally
		{
			_semaphore.Release();
		}
	}

	protected virtual void Dispose(bool disposing)
	{
		if (!_disposed)
		{
			if (disposing)
				_semaphore.Dispose();

			_disposed = true;
		}
	}

	public void Dispose()
	{
		Dispose(true);
		GC.SuppressFinalize(this);
	}
}
