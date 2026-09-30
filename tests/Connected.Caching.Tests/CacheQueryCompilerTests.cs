using Connected.Caching;
using System.Diagnostics;
using System.Linq.Expressions;

namespace Connected.Caching.Tests;

/// <summary>
/// Holds that a cache query run through its compiled shape returns exactly what it returned through
/// <see cref="EnumerableQuery{T}"/> - the path <c>CacheContainer</c> used on every call before.
/// </summary>
/// <remarks>
/// Every test runs the same query both ways over the same snapshot and compares the results element by element,
/// by reference for entries, so an entry is the same instance either way.
/// </remarks>
[TestClass]
[DoNotParallelize]
public sealed class CacheQueryCompilerTests
{
	public sealed class Entry
	{
		public int Id { get; init; }
		public string? Code { get; init; }
		public int Type { get; init; }
		public int? Parent { get; init; }
		public DateTimeOffset? ValidFrom { get; init; }
		public DateTimeOffset? ValidTo { get; init; }
		public decimal Amount { get; init; }
		public List<string> Tags { get; init; } = [];
	}

	public sealed class OtherEntry
	{
		public int Id { get; init; }
		public string? Code { get; init; }
	}

	/// <summary>What a service's query closes over: a DTO it reads per element, as the tenant's queries do.</summary>
	public sealed class Filter
	{
		public List<int>? Ids { get; set; }
		public List<int>? Types { get; set; }
		public List<string>? Codes { get; set; }
		public List<int>? Parents { get; set; }
		public DateTimeOffset? ValidAt { get; set; }
		public int? Skip { get; set; }
		public int? Take { get; set; }
	}

	private int _threshold = 50;

	/// <summary>The cache's own queryable: a constant the query is rooted at, replaced by the snapshot.</summary>
	private static readonly IQueryable<Entry> Cache = new List<Entry>().AsQueryable();
	private static readonly IQueryable<OtherEntry> OtherCache = new List<OtherEntry>().AsQueryable();

	[TestInitialize]
	public void Initialize()
	{
		CacheQueryCompiler.Enabled = true;
		CacheQueryCompiler.Reset();
	}

	#region Filters as the tenant writes them

	[TestMethod]
	public void FiltersOnListsInAClosedOverDtoMatch()
	{
		var data = Data(300, seed: 1);
		var filter = new Filter { Ids = [3, 8, 21, 144], Types = [1, 2] };

		Same(data, Cache.Where(f => filter.Ids!.Contains(f.Id)).Where(f => filter.Types!.Contains(f.Type)));
	}

	[TestMethod]
	public void ContainsWithAStaticComparerMatches()
	{
		var data = Data(300, seed: 2);
		var filter = new Filter { Codes = ["c-001", "C-017", "c-XYZ", "C-120"] };

		Same(data, Cache.Where(f => filter.Codes!.Contains(f.Code, StringComparer.OrdinalIgnoreCase)));
	}

	[TestMethod]
	public void NullableMembersMatch()
	{
		var data = Data(300, seed: 3);
		var filter = new Filter { Parents = [1, 2, 3, 4] };

		Same(data, Cache.Where(f => f.Parent != null && filter.Parents!.Contains(f.Parent.Value)));
	}

	[TestMethod]
	public void OpenTemporalBoundsMatch()
	{
		var data = Data(300, seed: 4);
		var filter = new Filter { ValidAt = new DateTimeOffset(2026, 3, 1, 0, 0, 0, TimeSpan.Zero) };

		Same(data, Cache.Where(f => (f.ValidFrom == null || f.ValidFrom <= filter.ValidAt) && (f.ValidTo == null || f.ValidTo >= filter.ValidAt)));
	}

	[TestMethod]
	public void OrderingAndPagingMatch()
	{
		var data = Data(300, seed: 5);
		var filter = new Filter { Skip = 10, Take = 25 };

		Same(data, Cache.OrderBy(f => f.Type).ThenByDescending(f => f.Code).ThenBy(f => f.Id).Skip(filter.Skip!.Value).Take(filter.Take!.Value));
	}

	[TestMethod]
	public void LiteralConstantsShareAShapeAndStillDiffer()
	{
		var data = Data(200, seed: 6);

		Same(data, Cache.Where(f => f.Type == 1));
		Same(data, Cache.Where(f => f.Type == 2));

		Assert.AreEqual(1, CacheQueryCompiler.ShapeCount);
	}

	/// <summary>
	/// Constants of several types in one query - literals, a closure, paging values - each land in their own place, for
	/// two sets of values sharing one shape.
	/// </summary>
	[TestMethod]
	public void ManyConstantsKeepTheirPlaces()
	{
		var data = Data(300, seed: 16);

		foreach (var (type, code, amount, skip, take) in new[] { (3, "C-001", 10m, 2, 40), (1, "c-050", 55.5m, 0, 7) })
		{
			var filter = new Filter { Parents = [1, 2] };

			Same(data, Cache
				.Where(f => f.Type == type && f.Code != code)
				.Where(f => f.Amount > amount && (f.Parent == null || filter.Parents!.Contains(f.Parent.Value)))
				.OrderBy(f => f.Id)
				.Skip(skip)
				.Take(take));
		}

		Assert.AreEqual(1, CacheQueryCompiler.ShapeCount);
	}

	[TestMethod]
	public void AnInstanceMemberOfTheCallerMatches()
	{
		var data = Data(200, seed: 7);

		Same(data, Cache.Where(f => f.Amount > _threshold));
		_threshold = 90;
		Same(data, Cache.Where(f => f.Amount > _threshold));
	}

	#endregion

	#region Scalars and projections

	[TestMethod]
	public void ScalarsMatch()
	{
		var data = Data(250, seed: 8);
		var filter = new Filter { Types = [2] };

		SameScalar<bool>(data, Cache.Where(f => filter.Types!.Contains(f.Type)).Expression, q => q.Any());
		SameScalar<int>(data, Cache.Expression, q => q.Count(f => f.Parent == null));
		SameScalar<decimal>(data, Cache.Expression, q => q.Sum(f => f.Amount));
		SameScalar<decimal>(data, Cache.Expression, q => q.Max(f => f.Amount));
		SameScalar<int>(data, Cache.Expression, q => q.Min(f => f.Id));
		SameScalar<decimal>(data, Cache.Expression, q => q.Average(f => f.Amount));
		SameScalar<Entry?>(data, Cache.Expression, q => q.OrderBy(f => f.Id).FirstOrDefault(f => f.Type == 3));
		SameScalar<Entry?>(data, Cache.Expression, q => q.FirstOrDefault(f => f.Id < 0));
	}

	[TestMethod]
	public void ProjectionsMatch()
	{
		var data = Data(200, seed: 9);

		SameValues(data, Cache.Where(f => f.Type == 1).Select(f => new { f.Id, f.Code, Doubled = f.Amount * 2 }));
		SameValues(data, Cache.Select(f => f.Code).Distinct());
		SameValues(data, Cache.SelectMany(f => f.Tags).Distinct().OrderBy(f => f));
		SameValues(data, Cache.GroupBy(f => f.Type).Select(g => new { g.Key, Count = g.Count(), Total = g.Sum(f => f.Amount) }).OrderBy(f => f.Key));
	}

	#endregion

	#region Behaviour around values and time

	/// <summary>The same shape reused with other values reads the new values, and compiles once.</summary>
	[TestMethod]
	public void TheSameShapeWithOtherValuesReadsTheNewValues()
	{
		var data = Data(300, seed: 10);

		foreach (var ids in new[] { new List<int> { 1, 2 }, new List<int> { 150, 151, 152 }, new List<int>() })
		{
			var filter = new Filter { Ids = ids };

			Same(data, Cache.Where(f => filter.Ids!.Contains(f.Id)));
		}

		Assert.AreEqual(1, CacheQueryCompiler.ShapeCount);
	}

	/// <summary>A closure changed after the query ran but before it was enumerated is read when enumerated, both ways.</summary>
	[TestMethod]
	public void EnumerationIsDeferredAsBefore()
	{
		var data = Data(200, seed: 11);
		var filter = new Filter { Types = [1] };
		var query = Cache.Where(f => filter.Types!.Contains(f.Type));

		var before = Old<IEnumerable<Entry>>(data, query.Expression);
		Assert.IsTrue(CacheQueryCompiler.TryExecute<Entry, IEnumerable<Entry>>(query.Expression, data, out var after));

		filter.Types = [2];

		CollectionAssert.AreEqual(before.ToList(), after.ToList());
		Assert.IsTrue(after.All(f => f.Type == 2));
	}

	/// <summary>An exception a query throws is the same exception, and an empty snapshot throws nothing, both ways.</summary>
	[TestMethod]
	public void ExceptionsMatch()
	{
		var data = Data(50, seed: 12);
		var empty = new List<Entry>();
		var query = Cache.Where(f => f.Code!.Length > 3);

		var old = Assert.ThrowsException<NullReferenceException>(() => Old<IEnumerable<Entry>>(data, query.Expression).ToList());
		var compiled = Assert.ThrowsException<NullReferenceException>(() =>
		{
			Assert.IsTrue(CacheQueryCompiler.TryExecute<Entry, IEnumerable<Entry>>(query.Expression, data, out var result));
			_ = result.ToList();
		});

		Assert.AreEqual(old.GetType(), compiled.GetType());
		Same(empty, query);
		Assert.ThrowsException<InvalidOperationException>(() => Old<Entry>(empty, Expression.Call(typeof(Queryable), nameof(Queryable.First), [typeof(Entry)], Cache.Expression)));
		Assert.ThrowsException<InvalidOperationException>(() =>
			CacheQueryCompiler.TryExecute<Entry, Entry>(Expression.Call(typeof(Queryable), nameof(Queryable.First), [typeof(Entry)], Cache.Expression), empty, out _));
	}

	#endregion

	#region What falls back

	/// <summary>
	/// A query reading another queryable through a closure runs that queryable the way the old path did: its
	/// <see cref="Queryable"/> call rewritten to <see cref="Enumerable"/> over it, both ways.
	/// </summary>
	[TestMethod]
	public void AnotherQueryableReadThroughAClosureMatches()
	{
		var data = Data(80, seed: 13);
		var others = Enumerable.Range(20, 30).Select(f => new OtherEntry { Id = f, Code = $"o-{f}" }).ToList().AsQueryable();

		Same(data, Cache.Where(f => others.Any(o => o.Id == f.Id)));
	}

	/// <summary>Another queryable standing in the tree as a constant is the one thing the old path unwraps specially; the
	/// compiled path leaves it to the old one.</summary>
	[TestMethod]
	public void AnotherQueryableAsAConstantFallsBack()
	{
		var data = Data(50, seed: 13);
		var entry = Expression.Parameter(typeof(Entry), "f");
		var other = Expression.Parameter(typeof(OtherEntry), "o");
		var anyOther = Expression.Call(typeof(Queryable), nameof(Queryable.Any), [typeof(OtherEntry)], Expression.Constant(OtherCache),
			Expression.Quote(Expression.Lambda<Func<OtherEntry, bool>>(
				Expression.Equal(Expression.Property(other, nameof(OtherEntry.Id)), Expression.Property(entry, nameof(Entry.Id))), other)));
		var query = Expression.Call(typeof(Queryable), nameof(Queryable.Where), [typeof(Entry)], Cache.Expression,
			Expression.Quote(Expression.Lambda<Func<Entry, bool>>(anyOther, entry)));

		Assert.IsFalse(CacheQueryCompiler.TryExecute<Entry, IEnumerable<Entry>>(query, data, out _));
		Assert.AreEqual(0, Old<IEnumerable<Entry>>(data, query).Count());
	}

	[TestMethod]
	public void TurnedOffEverythingFallsBack()
	{
		CacheQueryCompiler.Enabled = false;

		Assert.IsFalse(CacheQueryCompiler.TryExecute<Entry, IEnumerable<Entry>>(Cache.Where(f => f.Type == 1).Expression, Data(10, seed: 14), out _));
	}

	[TestMethod]
	public void ShapesOfOtherEntryTypesDoNotCollide()
	{
		var others = Enumerable.Range(1, 40).Select(f => new OtherEntry { Id = f, Code = $"o-{f}" }).ToList();
		var data = Data(40, seed: 15);

		Same(data, Cache.Where(f => f.Id > 20));
		var query = OtherCache.Where(f => f.Id > 20);
		var old = Old<OtherEntry, IEnumerable<OtherEntry>>(others, query.Expression, OtherCache).ToList();

		Assert.IsTrue(CacheQueryCompiler.TryExecute<OtherEntry, IEnumerable<OtherEntry>>(query.Expression, others, out var compiled));
		CollectionAssert.AreEqual(old, compiled.ToList());
		Assert.AreEqual(2, CacheQueryCompiler.ShapeCount);
	}

	#endregion

	#region Randomised

	/// <summary>Five hundred generated queries - filters, orderings, paging and scalars in random combination over random
	/// snapshots - each compared both ways.</summary>
	[TestMethod]
	public void GeneratedQueriesMatch()
	{
		var random = new Random(20260930);

		for (var run = 0; run < 500; run++)
		{
			var data = Data(random.Next(0, 400), seed: random.Next());
			var filter = new Filter
			{
				Ids = random.Next(3) == 0 ? null : [.. Enumerable.Range(0, random.Next(0, 6)).Select(_ => random.Next(0, 420))],
				Types = random.Next(3) == 0 ? null : [.. Enumerable.Range(0, random.Next(0, 3)).Select(_ => random.Next(0, 5))],
				Codes = random.Next(3) == 0 ? null : [.. Enumerable.Range(0, random.Next(0, 4)).Select(_ => $"C-{random.Next(0, 420):000}")],
				ValidAt = random.Next(2) == 0 ? null : new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero).AddDays(random.Next(0, 365)),
				Skip = random.Next(0, 30),
				Take = random.Next(1, 60)
			};

			var query = Cache.AsQueryable();

			if (filter.Ids is not null)
				query = query.Where(f => filter.Ids.Contains(f.Id));

			if (filter.Types is not null)
				query = query.Where(f => filter.Types.Contains(f.Type));

			if (filter.Codes is not null)
				query = query.Where(f => filter.Codes.Contains(f.Code, StringComparer.OrdinalIgnoreCase));

			if (filter.ValidAt is not null)
				query = query.Where(f => (f.ValidFrom == null || f.ValidFrom <= filter.ValidAt) && (f.ValidTo == null || f.ValidTo >= filter.ValidAt));

			switch (random.Next(4))
			{
				case 0:
					Same(data, query);
					break;
				case 1:
					Same(data, query.OrderBy(f => f.Code).ThenBy(f => f.Id).Skip(filter.Skip!.Value).Take(filter.Take!.Value));
					break;
				case 2:
					SameScalar<int>(data, query.Expression, q => q.Count());
					break;
				default:
					SameScalar<Entry?>(data, query.Expression, q => q.OrderByDescending(f => f.Amount).ThenBy(f => f.Id).FirstOrDefault());
					break;
			}
		}

		Assert.IsTrue(CacheQueryCompiler.ShapeCount <= 64, $"{CacheQueryCompiler.ShapeCount} shapes for 500 queries of 16 kinds");
	}

	#endregion

	#region Concurrency and counting

	/// <summary>
	/// The shapes a cache serves, as builders over their own values - the way concurrent requests build them.
	/// </summary>
	private static readonly Func<Random, Expression>[] ConcurrentShapes =
	[
		r => { var f = new Filter { Ids = [r.Next(0, 300), r.Next(0, 300)] }; return Cache.Where(e => f.Ids!.Contains(e.Id)).Expression; },
		r => { var f = new Filter { Types = [r.Next(0, 5)] }; return Cache.Where(e => f.Types!.Contains(e.Type)).Expression; },
		r => { var f = new Filter { Codes = [$"c-{r.Next(0, 300):000}"] }; return Cache.Where(e => f.Codes!.Contains(e.Code, StringComparer.OrdinalIgnoreCase)).Expression; },
		r => { var f = new Filter { Parents = [r.Next(0, 6)] }; return Cache.Where(e => e.Parent != null && f.Parents!.Contains(e.Parent.Value)).Expression; },
		r => { var f = new Filter { ValidAt = new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero).AddDays(r.Next(0, 365)) };
			return Cache.Where(e => (e.ValidFrom == null || e.ValidFrom <= f.ValidAt) && (e.ValidTo == null || e.ValidTo >= f.ValidAt)).Expression; },
		r => { var skip = r.Next(0, 50); var take = r.Next(1, 40); return Cache.OrderBy(e => e.Code).ThenBy(e => e.Id).Skip(skip).Take(take).Expression; },
		r => { var t = r.Next(0, 5); return Cache.Where(e => e.Type == t).OrderByDescending(e => e.Amount).ThenBy(e => e.Id).Expression; },
		r => { var min = (decimal)r.Next(0, 100); return Cache.Where(e => e.Amount >= min).Expression; }
	];

	/// <summary>
	/// Many threads running the same shapes at once, from an empty cache of shapes so they race on the first
	/// compilation, each over its own values - every result compared with the old path.
	/// </summary>
	[TestMethod]
	public void ConcurrentQueriesMatch()
	{
		var shared = Data(300, seed: 30);
		var failures = new System.Collections.Concurrent.ConcurrentBag<string>();
		const int queries = 8_000;

		Parallel.For(0, queries, new ParallelOptions { MaxDegreeOfParallelism = Math.Max(4, Environment.ProcessorCount * 2) }, i =>
		{
			var random = new Random(i);
			/*
			 * Half the queries share one snapshot, half run over a snapshot of their own, as a scope with pending rows
			 * does: a result must come from the snapshot its own call passed in.
			 */
			var data = i % 2 == 0 ? shared : Data(random.Next(0, 120), seed: i);
			var expression = ConcurrentShapes[i % ConcurrentShapes.Length](random);

			try
			{
				var old = Old<IEnumerable<Entry>>(data, expression).ToList();

				if (!CacheQueryCompiler.TryExecute<Entry, IEnumerable<Entry>>(expression, data, out var compiled))
				{
					failures.Add($"query {i}: declined");

					return;
				}

				var list = compiled.ToList();

				if (list.Count != old.Count || list.Where((f, index) => !ReferenceEquals(f, old[index])).Any())
					failures.Add($"query {i}: {list.Count} entries against {old.Count}");
			}
			catch (Exception ex)
			{
				failures.Add($"query {i}: {ex.GetType().Name} {ex.Message}");
			}
		});

		Assert.AreEqual(0, failures.Count, string.Join(Environment.NewLine, failures.Take(10)));
		Assert.AreEqual(ConcurrentShapes.Length, CacheQueryCompiler.ShapeCount);
		Assert.AreEqual(queries, CacheQueryCompiler.CompiledExecutions);
		Assert.AreEqual(0, CacheQueryCompiler.FallbackExecutions);
	}

	/// <summary>Every execution is counted on the path it took, a fallback with its reason.</summary>
	[TestMethod]
	public void ExecutionsAreCountedByPathAndReason()
	{
		var data = Data(30, seed: 31);
		var entry = Expression.Parameter(typeof(Entry), "f");
		var other = Expression.Parameter(typeof(OtherEntry), "o");
		var constantOther = Expression.Call(typeof(Queryable), nameof(Queryable.Where), [typeof(Entry)], Cache.Expression,
			Expression.Quote(Expression.Lambda<Func<Entry, bool>>(
				Expression.Call(typeof(Queryable), nameof(Queryable.Any), [typeof(OtherEntry)], Expression.Constant(OtherCache),
					Expression.Quote(Expression.Lambda<Func<OtherEntry, bool>>(Expression.Constant(true), other))), entry)));

		for (var i = 0; i < 3; i++)
			CacheQueryCompiler.TryExecute<Entry, IEnumerable<Entry>>(Cache.Where(f => f.Type == i).Expression, data, out _);

		for (var i = 0; i < 2; i++)
			CacheQueryCompiler.TryExecute<Entry, IEnumerable<Entry>>(constantOther, data, out _);

		CacheQueryCompiler.Enabled = false;
		CacheQueryCompiler.TryExecute<Entry, IEnumerable<Entry>>(Cache.Where(f => f.Type == 1).Expression, data, out _);
		CacheQueryCompiler.Enabled = true;

		Assert.AreEqual(3, CacheQueryCompiler.CompiledExecutions);
		Assert.AreEqual(3, CacheQueryCompiler.FallbackExecutions);
		Assert.AreEqual(2, CacheQueryCompiler.Fallbacks["other-queryable"]);
		Assert.AreEqual(1, CacheQueryCompiler.Fallbacks["disabled"]);
	}

	#endregion

	#region Benchmark

	/// <summary>Times the old and the compiled path on the shape a tenant lookup has; prints, asserts nothing about speed.</summary>
	[TestMethod]
	[TestCategory("Benchmark")]
	public void Benchmark()
	{
		foreach (var size in new[] { 100, 1_000, 5_000 })
		{
			var data = Data(size, seed: size);
			var filter = new Filter { Types = [1, 2], Codes = ["C-010"], ValidAt = new DateTimeOffset(2026, 6, 1, 0, 0, 0, TimeSpan.Zero) };
			var query = Cache
				.Where(f => (f.ValidFrom == null || f.ValidFrom <= filter.ValidAt) && (f.ValidTo == null || f.ValidTo >= filter.ValidAt))
				.Where(f => filter.Types!.Contains(f.Type))
				.Where(f => filter.Codes!.Contains(f.Code, StringComparer.OrdinalIgnoreCase));
			const int calls = 300;

			for (var i = 0; i < 20; i++)
			{
				Old<IEnumerable<Entry>>(data, query.Expression).ToList();
				CacheQueryCompiler.TryExecute<Entry, IEnumerable<Entry>>(query.Expression, data, out var warm);
				warm.ToList();
			}

			var watch = Stopwatch.StartNew();

			for (var i = 0; i < calls; i++)
				Old<IEnumerable<Entry>>(data, query.Expression).ToList();

			var old = watch.Elapsed.TotalMilliseconds / calls;

			watch.Restart();

			for (var i = 0; i < calls; i++)
			{
				CacheQueryCompiler.TryExecute<Entry, IEnumerable<Entry>>(query.Expression, data, out var result);
				result.ToList();
			}

			var compiled = watch.Elapsed.TotalMilliseconds / calls;

			var line = $"BENCH {size,5} rows: old {old,8:0.000} ms  compiled {compiled,8:0.000} ms  x{old / compiled,6:0.0}";

			Console.WriteLine(line);
			File.AppendAllText(Path.Combine(AppContext.BaseDirectory, "benchmark.txt"), line + Environment.NewLine);
		}
	}

	/// <summary>
	/// Queries per second with 1, 4 and 8 threads running the tenant-lookup shape concurrently, both ways; prints,
	/// asserts nothing about speed.
	/// </summary>
	[TestMethod]
	[TestCategory("Benchmark")]
	public void ParallelThroughput()
	{
		var data = Data(1_000, seed: 40);
		var filter = new Filter { Types = [1, 2], Codes = ["C-010"], ValidAt = new DateTimeOffset(2026, 6, 1, 0, 0, 0, TimeSpan.Zero) };
		var query = Cache
			.Where(f => (f.ValidFrom == null || f.ValidFrom <= filter.ValidAt) && (f.ValidTo == null || f.ValidTo >= filter.ValidAt))
			.Where(f => filter.Types!.Contains(f.Type))
			.Where(f => filter.Codes!.Contains(f.Code, StringComparer.OrdinalIgnoreCase));
		const int perThread = 400;

		foreach (var threads in new[] { 1, 4, 8 })
		{
			double Rate(Action run)
			{
				run();

				var watch = Stopwatch.StartNew();

				Parallel.For(0, threads, new ParallelOptions { MaxDegreeOfParallelism = threads }, _ =>
				{
					for (var i = 0; i < perThread; i++)
						run();
				});

				return threads * perThread / watch.Elapsed.TotalSeconds;
			}

			var old = Rate(() => Old<IEnumerable<Entry>>(data, query.Expression).ToList());
			var compiled = Rate(() =>
			{
				CacheQueryCompiler.TryExecute<Entry, IEnumerable<Entry>>(query.Expression, data, out var result);
				result.ToList();
			});
			var line = $"PARALLEL {threads} threads: old {old,9:0} q/s  compiled {compiled,9:0} q/s  x{compiled / old,6:0.0}";

			Console.WriteLine(line);
			File.AppendAllText(Path.Combine(AppContext.BaseDirectory, "benchmark.txt"), line + Environment.NewLine);
		}
	}

	#endregion

	private static List<Entry> Data(int count, int seed)
	{
		var random = new Random(seed);
		var start = new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);

		return [.. Enumerable.Range(0, count).Select(i => new Entry
		{
			Id = i,
			Code = random.Next(10) == 0 ? null : (random.Next(2) == 0 ? $"C-{i:000}" : $"c-{i:000}"),
			Type = random.Next(0, 5),
			Parent = random.Next(3) == 0 ? null : random.Next(0, 6),
			ValidFrom = random.Next(4) == 0 ? null : start.AddDays(random.Next(0, 200)),
			ValidTo = random.Next(3) == 0 ? null : start.AddDays(random.Next(150, 365)),
			Amount = Math.Round((decimal)random.NextDouble() * 100, 2),
			Tags = [.. Enumerable.Range(0, random.Next(0, 3)).Select(t => $"t{random.Next(0, 6)}")]
		})];
	}

	private static void Same(List<Entry> data, IQueryable<Entry> query)
	{
		var old = Old<IEnumerable<Entry>>(data, query.Expression).ToList();

		Assert.IsTrue(CacheQueryCompiler.TryExecute<Entry, IEnumerable<Entry>>(query.Expression, data, out var compiled), "compiled path declined");

		var list = compiled.ToList();

		Assert.AreEqual(old.Count, list.Count, "count");

		for (var i = 0; i < old.Count; i++)
			Assert.AreSame(old[i], list[i], $"element {i}");
	}

	private static void SameValues<T>(List<Entry> data, IQueryable<T> query)
	{
		var old = Old<Entry, IEnumerable<T>>(data, query.Expression, Cache).ToList();

		Assert.IsTrue(CacheQueryCompiler.TryExecute<Entry, IEnumerable<T>>(query.Expression, data, out var compiled), "compiled path declined");

		var list = compiled.ToList();

		Assert.AreEqual(old.Count, list.Count, "count");

		for (var i = 0; i < old.Count; i++)
			Assert.AreEqual(old[i], list[i], $"element {i}");
	}

	private static void SameScalar<T>(List<Entry> data, Expression source, Expression<Func<IQueryable<Entry>, T>> scalar)
	{
		var call = new SourceBinder(scalar.Parameters[0], source).Visit(scalar.Body);
		var old = Old<T>(data, call);

		Assert.IsTrue(CacheQueryCompiler.TryExecute<Entry, T>(call, data, out var compiled), "compiled path declined");

		if (old is Entry || compiled is Entry)
			Assert.AreSame(old, compiled);
		else
			Assert.AreEqual(old, compiled);
	}

	private static TResult Old<TResult>(List<Entry> data, Expression expression) => Old<Entry, TResult>(data, expression, Cache);

	/// <summary>The path <c>CacheContainer</c> took on every call: the snapshot as an <see cref="EnumerableQuery{T}"/>, the
	/// cache's queryable replaced by it, executed.</summary>
	private static TResult Old<T, TResult>(List<T> data, Expression expression, IQueryable<T> cache)
	{
		var snapshot = data.AsQueryable();
		var rewritten = new StaleSourceReplacer<T>(snapshot).Visit(expression);

		return snapshot.Provider.Execute<TResult>(rewritten);
	}

	private sealed class StaleSourceReplacer<T>(IQueryable<T> snapshot) : ExpressionVisitor
	{
		protected override Expression VisitConstant(ConstantExpression node)
			=> node.Value is IQueryable<T> ? snapshot.Expression : base.VisitConstant(node);
	}

	private sealed class SourceBinder(ParameterExpression parameter, Expression source) : ExpressionVisitor
	{
		protected override Expression VisitParameter(ParameterExpression node) => node == parameter ? source : node;
	}
}
