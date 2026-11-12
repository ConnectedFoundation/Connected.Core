namespace Connected.Caching.Tests;

/// <summary>
/// Holds that entries written from many threads at once into a key that does not exist yet are all kept,
/// including those written by the threads that lost the race to create the key's bucket (RA-496).
/// </summary>
[TestClass]
public sealed class CacheScopeConcurrencyTests
{
	private const int Threads = 64;
	private const int Rounds = 200;

	[TestMethod]
	public async Task CopyToKeepsEveryEntryWhenKeyIsCreatedConcurrently()
	{
		await Hammer((scope, key, id) => scope.CopyTo(key, id, new Entry(id.ToString(), id, TimeSpan.Zero, false, CacheEntryMergeBehavior.Merge)));
	}

	[TestMethod]
	public async Task SetKeepsEveryEntryWhenKeyIsCreatedConcurrently()
	{
		await Hammer((scope, key, id) => scope.Set(key, id, (object)id, TimeSpan.Zero, false, CacheEntryMergeBehavior.Merge));
	}

	/// <summary>
	/// Writers keep emptying their key and writing to it again while the cleanup removes empty keys. A write that
	/// lands in a key's bucket just as the cleanup removes it must not be lost.
	/// </summary>
	[TestMethod]
	public async Task ScavengeKeepsEveryEntryWrittenIntoKeyItRemoves()
	{
		const int writers = 8;
		const int iterations = 100_000;

		using var scope = new CacheScope(false);
		using var done = new CancellationTokenSource();
		var lost = 0;

		var scavengers = Enumerable.Range(0, 2).Select(_ => Task.Factory.StartNew(() =>
		{
			while (!done.IsCancellationRequested)
				scope.Scavenge();
		}, CancellationToken.None, TaskCreationOptions.LongRunning, TaskScheduler.Default)).ToArray();

		var tasks = Enumerable.Range(0, writers).Select(w => Task.Factory.StartNew(() =>
		{
			var key = $"key-{w % 2}";

			for (var i = 0; i < iterations; i++)
			{
				var id = $"{w}-{i}";

				if (i % 2 == 0)
					scope.Set(key, id, (object)id);
				else
					scope.CopyTo(key, id, new Entry(id, id, TimeSpan.Zero, false, CacheEntryMergeBehavior.Merge));

				if (!scope.Ids(key).Contains(id))
					Interlocked.Increment(ref lost);

				scope.Remove(key, id).GetAwaiter().GetResult();
			}
		}, CancellationToken.None, TaskCreationOptions.LongRunning, TaskScheduler.Default)).ToArray();

		await Task.WhenAll(tasks);
		done.Cancel();
		await Task.WhenAll(scavengers);

		Assert.AreEqual(0, lost, $"{lost} of {writers * iterations} entries lost to the cleanup.");
	}

	private static async Task Hammer(Action<CacheScope, string, int> write)
	{
		using var scope = new CacheScope(false);

		for (var round = 0; round < Rounds; round++)
		{
			var key = $"key-{round}";
			using var start = new ManualResetEventSlim(false);
			var tasks = new Task[Threads];

			for (var i = 0; i < Threads; i++)
			{
				var id = i;

				tasks[i] = Task.Factory.StartNew(() =>
				{
					start.Wait();
					write(scope, key, id);
				}, CancellationToken.None, TaskCreationOptions.LongRunning, TaskScheduler.Default);
			}

			start.Set();
			await Task.WhenAll(tasks);

			var ids = scope.Ids(key);
			var missing = Enumerable.Range(0, Threads).Where(f => !ids.Contains(f.ToString())).ToList();

			Assert.AreEqual(0, missing.Count, $"Round {round}: {missing.Count} of {Threads} entries lost ({string.Join(", ", missing)}).");
		}
	}
}
