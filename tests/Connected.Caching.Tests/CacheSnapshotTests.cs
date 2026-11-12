using System.Collections.Concurrent;
using System.Collections.Immutable;
using System.Diagnostics;

namespace Connected.Caching.Tests;

/// <summary>
/// Holds that a bucket's snapshot is built once and handed out until the bucket changes, that a change is seen by the
/// next query, and that a snapshot already handed out never changes (RA-480).
/// </summary>
[TestClass]
public sealed class CacheSnapshotTests
{
	private const string Key = "bucket";

	public TestContext TestContext { get; set; } = default!;

	[TestMethod]
	public void AllReturnsTheSameSnapshotWhileNothingChanges()
	{
		using var scope = Fill(3);

		var first = scope.All<string>(Key);
		var second = scope.All<string>(Key);

		Assert.AreSame(first, second);
		Assert.AreEqual(3, first.Count);
	}

	[TestMethod]
	public void SetGivesTheNextQueryANewSnapshotAndLeavesTheHeldOneAlone()
	{
		using var scope = Fill(3);

		var held = scope.All<string>(Key);

		scope.Set(Key, "new", "new");

		var next = scope.All<string>(Key);

		Assert.AreNotSame(held, next);
		Assert.AreEqual(3, held.Count);
		Assert.IsFalse(held.Contains("new"));
		Assert.IsTrue(next.Contains("new"));
	}

	[TestMethod]
	public void ReplacingAnEntryLeavesTheHeldSnapshotWithTheOldInstance()
	{
		using var scope = Fill(3);

		var held = scope.All<string>(Key);

		scope.Set(Key, "1", "replaced");

		Assert.IsTrue(held.Contains("1"));
		Assert.IsFalse(held.Contains("replaced"));
		Assert.IsTrue(scope.All<string>(Key).Contains("replaced"));
	}

	[TestMethod]
	public async Task RemoveGivesTheNextQueryANewSnapshot()
	{
		using var scope = Fill(3);

		var held = scope.All<string>(Key);

		await scope.Remove(Key, "1");

		var next = scope.All<string>(Key);

		Assert.AreEqual(3, held.Count);
		Assert.AreEqual(2, next.Count);
		Assert.IsFalse(next.Contains("1"));
	}

	[TestMethod]
	public void ScavengeOfAnExpiredEntryGivesTheNextQueryANewSnapshot()
	{
		using var scope = Fill(3);

		scope.Set(Key, "short", "short", TimeSpan.FromMilliseconds(1));

		var held = scope.All<string>(Key);

		Thread.Sleep(20);
		scope.Scavenge();

		var next = scope.All<string>(Key);

		Assert.AreEqual(4, held.Count);
		Assert.AreEqual(3, next.Count);
		Assert.IsFalse(next.Contains("short"));
	}

	[TestMethod]
	public void SnapshotsArePerRequestedType()
	{
		using var scope = Fill(3);

		var asString = scope.All<string>(Key);
		var asObject = scope.All<object>(Key);

		Assert.AreSame(asString, scope.All<string>(Key));
		Assert.AreSame(asObject, scope.All<object>(Key));
		Assert.ThrowsException<InvalidCastException>(() => scope.All<Uri>(Key));
	}

	[TestMethod]
	public void WhereAndGetReadTheCurrentEntries()
	{
		using var scope = Fill(10);

		Assert.AreEqual(1, scope.Where<string>(Key, f => f == "5").Count);
		Assert.AreEqual("5", scope.Get<string>(Key, f => f == "5"));

		scope.Set(Key, "5", "five");

		Assert.AreEqual(0, scope.Where<string>(Key, f => f == "5").Count);
		Assert.AreEqual("five", scope.Get<string>(Key, f => f == "five"));
		Assert.AreEqual("five", scope.Get(Key, (Func<string, bool>)(f => f == "five"))!);
	}

	/// <summary>
	/// Writers add entries in order and drop the old ones behind them, while the cleanup expires entries in the same
	/// bucket and readers query it. Every snapshot must show each writer's entries as one unbroken run (a run with a gap
	/// would be a torn read), and every writer's last entries must be there at the end.
	/// </summary>
	[TestMethod]
	public async Task ReadersSeeConsistentSnapshotsWhileWritersAndTheCleanupRun()
	{
		const int writers = 4;
		const int readers = 4;
		const int iterations = 20_000;
		const int window = 10;

		using var scope = new CacheScope(false);
		using var done = new CancellationTokenSource();
		var errors = new ConcurrentQueue<string>();
		var reads = 0L;

		var scavengers = Enumerable.Range(0, 2).Select(_ => Run(() =>
		{
			while (!done.IsCancellationRequested)
			{
				scope.Set(Key, $"x-{Guid.NewGuid()}", "x", TimeSpan.FromTicks(1));
				scope.Scavenge();
			}
		})).ToArray();

		var readerTasks = Enumerable.Range(0, readers).Select(_ => Run(() =>
		{
			while (!done.IsCancellationRequested)
			{
				try
				{
					var snapshot = scope.All<string>(Key);
					var runs = snapshot.Where(f => f != "x").Select(Parse).GroupBy(f => f.Writer);

					foreach (var run in runs)
					{
						var indexes = run.Select(f => f.Index).ToList();

						if (indexes.Max() - indexes.Min() + 1 != indexes.Count)
							errors.Enqueue($"Writer {run.Key}: torn snapshot {string.Join(",", indexes.Order())}.");
					}

					Interlocked.Increment(ref reads);
				}
				catch (Exception ex)
				{
					errors.Enqueue(ex.ToString());
				}
			}
		})).ToArray();

		var writerTasks = Enumerable.Range(0, writers).Select(w => Run(() =>
		{
			for (var i = 0; i < iterations; i++)
			{
				var id = $"{w}-{i}";

				scope.Set(Key, id, id);

				if (i >= window)
					scope.Remove(Key, $"{w}-{i - window}").GetAwaiter().GetResult();
			}
		})).ToArray();

		await Task.WhenAll(writerTasks);
		done.Cancel();
		await Task.WhenAll(readerTasks.Concat(scavengers));

		var ids = scope.Ids(Key);
		var missing = Enumerable.Range(0, writers)
			.SelectMany(w => Enumerable.Range(iterations - window, window).Select(i => $"{w}-{i}"))
			.Where(f => !ids.Contains(f))
			.ToList();

		TestContext.WriteLine($"{reads} snapshot reads checked.");

		Assert.AreEqual(0, errors.Count, string.Join(Environment.NewLine, errors.Take(5)));
		Assert.AreEqual(0, missing.Count, $"Lost writes: {string.Join(", ", missing)}");
		Assert.IsTrue(reads > 0);
	}

	/// <summary>
	/// Not asserted: prints how long 1,000 queries over a 40,000 entry bucket take, copying the bucket on every query
	/// (the old All) against handing out the snapshot.
	/// </summary>
	[TestMethod]
	public void TimingOfAllOverALargeBucket()
	{
		const int entries = 40_000;
		const int calls = 1_000;

		using var scope = Fill(entries);
		var copy = new ConcurrentDictionary<string, IEntry>();

		for (var i = 0; i < entries; i++)
			copy[i.ToString()] = new Entry(i.ToString(), i.ToString(), TimeSpan.Zero, false, CacheEntryMergeBehavior.Merge);

		var watch = Stopwatch.StartNew();
		var total = 0;

		for (var i = 0; i < calls; i++)
			total += CopyEveryEntry<string>(copy).Count;

		var before = watch.Elapsed;

		watch.Restart();

		for (var i = 0; i < calls; i++)
			total += scope.All<string>(Key).Count;

		var after = watch.Elapsed;

		watch.Restart();

		for (var i = 0; i < calls; i++)
			total += scope.Get<string>(Key, f => f == "39999") is null ? 0 : 1;

		var find = watch.Elapsed;

		TestContext.WriteLine($"{calls} x All() over {entries} entries: copy {before.TotalMilliseconds:F0} ms, snapshot {after.TotalMilliseconds:F0} ms; {calls} x Get(predicate, last entry): {find.TotalMilliseconds:F0} ms ({total}).");
	}

	/// <summary>
	/// The old Entries.All: a new list from every entry, then a new immutable list, on every call.
	/// </summary>
	private static IImmutableList<T> CopyEveryEntry<T>(ConcurrentDictionary<string, IEntry> items)
	{
		var r = new List<T>();

		foreach (var i in items.Select(f => f.Value.Instance))
		{
			if (i is not T t)
				throw new InvalidCastException();

			r.Add(t);
		}

		return [.. r];
	}

	private static CacheScope Fill(int count)
	{
		var scope = new CacheScope(false);

		for (var i = 0; i < count; i++)
			scope.Set(Key, i.ToString(), i.ToString());

		return scope;
	}

	private static (int Writer, int Index) Parse(string value)
	{
		var dash = value.IndexOf('-');

		return (int.Parse(value[..dash]), int.Parse(value[(dash + 1)..]));
	}

	private static Task Run(Action action)
	{
		return Task.Factory.StartNew(action, CancellationToken.None, TaskCreationOptions.LongRunning, TaskScheduler.Default);
	}
}
