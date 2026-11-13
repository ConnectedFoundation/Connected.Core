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
	/// A list a query handed out stays exactly as it was, item for item, while other threads keep writing to and
	/// removing from the bucket, and it refuses to be changed by its holder.
	/// </summary>
	[TestMethod]
	public async Task HeldListsNeverChangeWhileOtherThreadsWrite()
	{
		const int writers = 4;
		const int holders = 8;
		const int iterations = 20_000;

		using var scope = Fill(50);
		using var done = new CancellationTokenSource();
		var errors = new ConcurrentQueue<string>();
		var checks = 0L;

		var holderTasks = Enumerable.Range(0, holders).Select(h => Run(() =>
		{
			while (!done.IsCancellationRequested)
			{
				try
				{
					var held = h % 2 == 0 ? scope.All<string>(Key) : scope.Where<string>(Key, f => f.Length > 0);
					var frozen = held.ToArray();

					if (held is IList<string> list)
					{
						try
						{
							list.Add("intruder");
							errors.Enqueue("A held list accepted Add.");
						}
						catch (NotSupportedException)
						{
						}
					}

					for (var i = 0; i < 20; i++)
					{
						Thread.SpinWait(200);

						if (held.Count != frozen.Length || !held.SequenceEqual(frozen))
						{
							errors.Enqueue($"Holder {h}: a held list changed from {frozen.Length} to {held.Count} items.");
							break;
						}

						Interlocked.Increment(ref checks);
					}
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
				scope.Set(Key, $"w{w}-{i}", $"w{w}-{i}");

				if (i % 2 == 1)
					scope.Remove(Key, $"w{w}-{i - 1}").GetAwaiter().GetResult();
			}
		})).ToArray();

		await Task.WhenAll(writerTasks);
		done.Cancel();
		await Task.WhenAll(holderTasks);

		TestContext.WriteLine($"{checks} checks of held lists.");

		Assert.AreEqual(0, errors.Count, string.Join(Environment.NewLine, errors.Take(5)));
		Assert.IsTrue(checks > 0);
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
	/// Once a type's snapshot is built, every change is applied to it in place. A list handed out before a new key, a
	/// replaced instance, a removal (which moves the last item into the removed slot) or an expiry stays exactly as it was,
	/// for every type it was asked for, and the next query shows the change.
	/// </summary>
	[TestMethod]
	public async Task HeldListsStayUnchangedThroughEveryKindOfChange()
	{
		using var scope = Fill(5);

		scope.Set(Key, "short", "short", TimeSpan.FromMilliseconds(1));

		var steps = new List<(string Name, Func<Task> Change, string[] Expected)>
		{
			("set a new key", () => { scope.Set(Key, "new", "new"); return Task.CompletedTask; }, ["0", "1", "2", "3", "4", "short", "new"]),
			("replace", () => { scope.Set(Key, "2", "two"); return Task.CompletedTask; }, ["0", "1", "two", "3", "4", "short", "new"]),
			("remove a middle key", () => scope.Remove(Key, "0"), ["1", "two", "3", "4", "short", "new"]),
			("remove the last key", () => scope.Remove(Key, "new"), ["1", "two", "3", "4", "short"]),
			("expire", () => { Thread.Sleep(20); scope.Scavenge(); return Task.CompletedTask; }, ["1", "two", "3", "4"]),
			("clear", () => scope.Clear(Key), [])
		};

		foreach (var (name, change, expected) in steps)
		{
			var heldStrings = scope.All<string>(Key);
			var heldObjects = scope.All<object>(Key);
			var frozenStrings = heldStrings.ToArray();
			var frozenObjects = heldObjects.ToArray();

			await change();

			CollectionAssert.AreEqual(frozenStrings, heldStrings.ToArray(), $"{name}: a held list of strings changed.");
			CollectionAssert.AreEqual(frozenObjects, heldObjects.ToArray(), $"{name}: a held list of objects changed.");
			CollectionAssert.AreEquivalent(expected, scope.All<string>(Key).ToArray(), $"{name}: strings.");
			CollectionAssert.AreEquivalent(expected, scope.All<object>(Key).Cast<string>().ToArray(), $"{name}: objects.");
			Assert.AreEqual(expected.Length, scope.Where<string>(Key, f => true).Count, $"{name}: Where.");

			foreach (var i in expected)
				Assert.AreEqual(i, scope.Get<string>(Key, f => f == i), $"{name}: Get {i}.");
		}
	}

	/// <summary>
	/// An instance of another type, or null, in a bucket whose snapshot is built: the query as that type throws while the
	/// instance is there, as it always has, and works again once it is gone.
	/// </summary>
	[TestMethod]
	public async Task ForeignAndNullInstancesBehaveAsBefore()
	{
		using var scope = Fill(3);

		Assert.AreEqual(3, scope.All<string>(Key).Count);
		Assert.AreEqual(3, scope.All<object>(Key).Count);

		scope.Set<object>(Key, "number", 42);

		Assert.ThrowsException<InvalidCastException>(() => scope.All<string>(Key));
		Assert.AreEqual(4, scope.All<object>(Key).Count);

		await scope.Remove(Key, "number");

		Assert.AreEqual(3, scope.All<string>(Key).Count);

		scope.Set<string>(Key, "nothing", null);

		Assert.ThrowsException<InvalidCastException>(() => scope.All<string>(Key));
		Assert.ThrowsException<InvalidCastException>(() => scope.All<object>(Key));
		Assert.AreEqual(1, scope.Where<object>(Key, f => f is null).Count);

		scope.Set(Key, "nothing", "something");

		Assert.AreEqual(4, scope.All<string>(Key).Count);
		Assert.AreEqual(4, scope.All<object>(Key).Count);
	}

	/// <summary>
	/// A long random run of new keys, replacements, removals and expiries: after every step the snapshot of each type
	/// holds exactly the bucket's instances, as many as there are entries.
	/// </summary>
	[TestMethod]
	public void SnapshotsMatchTheBucketThroughARandomRunOfChanges()
	{
		const int steps = 20_000;
		const int keys = 300;

		var random = new Random(480);
		using var scope = Fill(50);
		var version = 0;

		scope.All<string>(Key);
		scope.All<object>(Key);

		for (var step = 0; step < steps; step++)
		{
			var key = random.Next(keys).ToString();

			switch (random.Next(10))
			{
				case < 5:
					scope.Set(Key, key, $"{key}-{version++}");
					break;
				case < 8:
					scope.Remove(Key, key).GetAwaiter().GetResult();
					break;
				case 8:
					scope.Set(Key, key, $"{key}-{version++}", TimeSpan.FromTicks(1));
					break;
				default:
					scope.Scavenge();
					break;
			}

			if (step % 10 != 0 && step != steps - 1)
				continue;

			var actual = scope.Ids(Key).Select(f => scope.Get<string>(Key, f)!).Order(StringComparer.Ordinal).ToList();
			var strings = scope.All<string>(Key).Order(StringComparer.Ordinal).ToList();
			var objects = scope.All<object>(Key).Cast<string>().Order(StringComparer.Ordinal).ToList();

			Assert.AreEqual(scope.Count(Key), strings.Count, $"Step {step}: count.");
			CollectionAssert.AreEqual(actual, strings, $"Step {step}: strings.");
			CollectionAssert.AreEqual(actual, objects, $"Step {step}: objects.");
			Assert.AreEqual(actual.Count, scope.Where<string>(Key, f => true).Count, $"Step {step}: Where.");
		}
	}

	/// <summary>
	/// Not asserted: the write-heavy pattern from the profile (RA-480). A 40,000 entry bucket, then 10,000 times a write
	/// followed by a query; then 10,000 writes followed by 10,000 queries. Written to ra480-timing.txt in the temp folder.
	/// </summary>
	[TestMethod]
	public void TimingOfWritesBetweenQueries()
	{
		const int entries = 40_000;
		const int iterations = 10_000;

		using var scope = Fill(entries);
		var total = 0L;

		scope.All<string>(Key);

		var watch = Stopwatch.StartNew();

		for (var i = 0; i < iterations; i++)
		{
			scope.Set(Key, (i % entries).ToString(), $"v{i}");
			total += scope.All<string>(Key).Count;
		}

		var interleaved = watch.Elapsed;

		watch.Restart();

		for (var i = 0; i < iterations; i++)
			scope.Set(Key, $"n{i}", $"n{i}");

		var writes = watch.Elapsed;

		watch.Restart();

		for (var i = 0; i < iterations; i++)
			total += scope.All<string>(Key).Count;

		var reads = watch.Elapsed;

		watch.Restart();

		var none = (Func<string, bool>)(f => ReferenceEquals(f, Key));

		for (var i = 0; i < 1_000; i++)
		{
			scope.Set(Key, (i % entries).ToString(), $"w{i}");
			total += scope.Get(Key, none) is null ? 0 : 1;
		}

		var scans = watch.Elapsed;

		watch.Restart();

		for (var i = 0; i < 1_000; i++)
			total += scope.Where(Key, none).Count;

		var readScans = watch.Elapsed;
		var line = $"{entries} entries: {iterations} x (Set, All) {interleaved.TotalMilliseconds:F0} ms; {iterations} Set {writes.TotalMilliseconds:F0} ms then {iterations} All {reads.TotalMilliseconds:F0} ms; 1000 x (Set, Get(predicate), full scan) {scans.TotalMilliseconds:F0} ms; 1000 x Where (full scan, no writes) {readScans.TotalMilliseconds:F0} ms ({total}).";

		TestContext.WriteLine(line);
		File.AppendAllText(Path.Combine(Path.GetTempPath(), "ra480-timing.txt"), line + Environment.NewLine);
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
