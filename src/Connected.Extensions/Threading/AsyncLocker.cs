using System.Collections.Concurrent;

namespace Connected.Threading;

public class AsyncLocker<T> : IDisposable
	where T : notnull
{
	public AsyncLocker()
	{
		Items = new();
	}

	private ConcurrentDictionary<T, AsyncLockerSlim> Items { get; }
	private bool IsDisposed { get; set; }

	public async Task LockAsync(T semaphore, Func<Task> worker)
	{
		if (Items.TryGetValue(semaphore, out AsyncLockerSlim? locker))
			await locker.LockAsync(worker);

		var newLocker = new AsyncLockerSlim();

		if (Items.TryAdd(semaphore, newLocker))
			await newLocker.LockAsync(worker);
		else
		{
			newLocker.Dispose();

			if (!Items.TryGetValue(semaphore, out AsyncLockerSlim? existing))
				throw new NullReferenceException(Strings.ErrLock);

			await existing.LockAsync(worker);
		}
	}

	public async Task<TReturnValue> LockAsync<TReturnValue>(T semaphore, Func<Task<TReturnValue>> worker)
	{
		if (Items.TryGetValue(semaphore, out AsyncLockerSlim? locker))
			return await locker.LockAsync(worker);

		var newLocker = new AsyncLockerSlim();

		if (Items.TryAdd(semaphore, newLocker))
			return await newLocker.LockAsync(worker);
		else
		{
			newLocker.Dispose();

			if (!Items.TryGetValue(semaphore, out AsyncLockerSlim? existing))
				throw new NullReferenceException(Strings.ErrLock);

			return await existing.LockAsync(worker);
		}
	}

	protected virtual void Dispose(bool disposing)
	{
		if (IsDisposed)
			return;

		if (disposing)
		{
			foreach (var slim in Items)
				slim.Value.Dispose();

			Items.Clear();
		}

		IsDisposed = true;
	}

	public void Dispose()
	{
		Dispose(true);
		GC.SuppressFinalize(this);
	}
}
