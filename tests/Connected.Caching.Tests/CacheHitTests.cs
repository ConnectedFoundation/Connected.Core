using System.Diagnostics;

namespace Connected.Caching.Tests;

/// <summary>
/// A read of a cached entry is a hit: the scavenger renews the entry's sliding expiration. The scavenger finds the
/// entry of each hit by its key, so its work grows with the hits, not with hits times entries.
/// </summary>
[TestClass]
public sealed class CacheHitTests
{
	public TestContext TestContext { get; set; } = default!;

	private sealed record Item(string Id, int Value);

	/// <summary>A bucket with one sliding entry, its expiration moved close so a renewal is visible.</summary>
	private static (Entries Entries, Entry Entry) Sliding(Item item)
	{
		var entries = new Entries();

		entries.Set(item.Id, item, TimeSpan.FromMinutes(10), slidingExpiration: true, CacheEntryMergeBehavior.Merge);

		var entry = (Entry)entries.Get(item.Id)!;

		entry.ExpirationDate = DateTime.UtcNow.AddSeconds(1);

		return (entries, entry);
	}

	private static void AssertRenewed(Entry entry) =>
		Assert.IsTrue(entry.ExpirationDate > DateTime.UtcNow.AddMinutes(5), $"Not renewed: {entry.ExpirationDate:O}");

	[TestMethod]
	public void A_read_by_key_renews_a_sliding_entry()
	{
		var item = new Item("a", 1);
		var (entries, entry) = Sliding(item);

		entry.ExpirationDate = DateTime.UtcNow.AddSeconds(1);
		_ = entries.Get(item.Id);
		entries.Scave();

		AssertRenewed(entry);
	}

	[TestMethod]
	public void A_read_by_predicate_renews_a_sliding_entry()
	{
		var item = new Item("a", 1);
		var (entries, entry) = Sliding(item);

		_ = entries.Get<Item>(f => f.Value == 1);
		entries.Scave();

		AssertRenewed(entry);
	}

	[TestMethod]
	public void A_query_renews_every_sliding_entry_it_returned()
	{
		var item = new Item("a", 1);
		var (entries, entry) = Sliding(item);

		Assert.AreEqual(1, entries.Where<Item>(f => f.Value == 1).Count);
		entries.Scave();

		AssertRenewed(entry);
	}

	[TestMethod]
	public void A_hit_on_an_instance_since_replaced_does_not_renew_its_replacement()
	{
		var old = new Item("a", 1);
		var (entries, _) = Sliding(old);

		_ = entries.Where<Item>(f => f.Value == 1);

		var replacement = new Item("a", 2);

		entries.Set(replacement.Id, replacement, TimeSpan.FromMinutes(10), slidingExpiration: true, CacheEntryMergeBehavior.Merge);

		var entry = (Entry)entries.Get(replacement.Id)!;
		var expiration = DateTime.UtcNow.AddSeconds(30);

		entry.ExpirationDate = expiration;
		/*
		 * The Get above is itself a hit on the replacement; drain it first so only the old hit is left.
		 */
		entries.Scave();
		entry.ExpirationDate = expiration;
		entries.Scave();

		Assert.AreEqual(expiration, entry.ExpirationDate);
	}

	[TestMethod]
	public void A_hit_on_an_entry_removed_since_is_dropped()
	{
		var item = new Item("a", 1);
		var (entries, _) = Sliding(item);

		_ = entries.Where<Item>(f => f.Value == 1);
		entries.Remove(item.Id);
		entries.Scave();

		Assert.IsNull(entries.Get(item.Id));
	}

	/// <summary>
	/// Not asserted: 20,000 entries, every one read once by a query, then one scavenge. Reported in the test output and in
	/// cache-hit-timing.txt in the temp folder.
	/// Finding each hit's entry by scanning the bucket made this quadratic.
	/// </summary>
	[TestMethod]
	public void Timing_scavenging_20k_hits_in_a_20k_bucket()
	{
		const int count = 20_000;
		var entries = new Entries();

		for (var i = 0; i < count; i++)
			entries.Set(i.ToString(), new Item(i.ToString(), i), TimeSpan.FromMinutes(10), true, CacheEntryMergeBehavior.Merge);

		Assert.AreEqual(count, entries.Where<Item>(f => true).Count);

		var watch = Stopwatch.StartNew();

		entries.Scave();
		var line = $"Scave of {count} hits in a {count} bucket: {watch.ElapsedMilliseconds} ms";

		TestContext.WriteLine(line);
		File.AppendAllText(Path.Combine(Path.GetTempPath(), "cache-hit-timing.txt"), line + Environment.NewLine);
	}
}
