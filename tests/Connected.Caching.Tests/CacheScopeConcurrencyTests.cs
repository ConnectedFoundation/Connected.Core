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
