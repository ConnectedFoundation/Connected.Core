using Connected.Annotations;
using Connected.Services;
using Microsoft.Extensions.DependencyInjection;
using System.ComponentModel.DataAnnotations;
using ValidationContext = Connected.Services.Validation.ValidationContext;

namespace Connected.Validation.Tests;

/// <summary>
/// How a request is walked before a service runs it: every complex member and every item of a collection is validated,
/// each object once, cycles end, and a member marked to be skipped is not read. The walk must stay linear in the number
/// of objects: a request carrying a large in-memory graph (100k items) spent hours deciding which objects it had seen.
/// </summary>
[TestClass]
public sealed class ValidationContextTests
{
	private static Task Validate(IDto dto)
	{
		var context = new ValidationContext(null!, new ServiceCollection().BuildServiceProvider());

		return context.Validate(null!, dto);
	}

	/// <summary>An item with one required member.</summary>
	private sealed class Item
	{
		[Required]
		public string? Name { get; set; } = "item";
	}

	/// <summary>A request holding a list of items.</summary>
	private sealed class ItemsDto : Dto
	{
		public List<Item> Items { get; set; } = [];
	}

	/// <summary>A request whose collection items are all valid passes.</summary>
	[TestMethod]
	public async Task A_graph_of_valid_items_passes() =>
		await Validate(new ItemsDto { Items = [new(), new(), new()] });

	/// <summary>An invalid item inside a collection fails the whole request, naming the member.</summary>
	[TestMethod]
	public async Task An_invalid_item_deep_in_a_collection_fails()
	{
		var ex = await Assert.ThrowsExceptionAsync<ValidationException>(() => Validate(new ItemsDto { Items = [new(), new() { Name = null }] }));

		StringAssert.Contains(ex.Message, "name");
	}

	/// <summary>A node that can point at another, so two can form a cycle.</summary>
	private sealed class Node
	{
		[Required]
		public string? Name { get; set; } = "node";

		public Node? Next { get; set; }
	}

	/// <summary>A request holding the first node.</summary>
	private sealed class NodeDto : Dto
	{
		public Node? Head { get; set; }
	}

	/// <summary>Two objects that refer to each other are each walked once and the walk ends.</summary>
	[TestMethod]
	public async Task A_reference_cycle_ends()
	{
		var a = new Node();
		var b = new Node { Next = a };

		a.Next = b;

		await Validate(new NodeDto { Head = a });
	}

	/// <summary>Ending a cycle does not skip validating the objects in it.</summary>
	[TestMethod]
	public async Task An_invalid_object_inside_a_cycle_still_fails()
	{
		var a = new Node();
		var b = new Node { Next = a, Name = null };

		a.Next = b;

		await Assert.ThrowsExceptionAsync<ValidationException>(() => Validate(new NodeDto { Head = a }));
	}

	/// <summary>
	/// A record whose member hands back a new copy of itself. The copy equals the original, which is what ends the walk;
	/// a walk that only remembered references would recurse until the stack ran out.
	/// </summary>
	private sealed record Echo
	{
		[Required]
		public string? Name { get; init; } = "echo";

		public Echo Copy => this with { };
	}

	/// <summary>A request holding an echo.</summary>
	private sealed class EchoDto : Dto
	{
		public Echo? Echo { get; set; }
	}

	/// <summary>A member handing back a new equal copy of its owner is not walked again (the copy equals an object above it).</summary>
	[TestMethod]
	public async Task A_member_that_returns_an_equal_copy_ends() =>
		await Validate(new EchoDto { Echo = new Echo() });

	/// <summary>An object whose member throws when read.</summary>
	private sealed class Unreadable
	{
		public string Value => throw new InvalidOperationException("read");
	}

	/// <summary>A request holding an unreadable object behind SkipValidation.</summary>
	private sealed class SkippedDto : Dto
	{
		[SkipValidation]
		public Unreadable Skipped { get; set; } = new();
	}

	/// <summary>A member marked SkipValidation is not read at all: reading this one would throw.</summary>
	[TestMethod]
	public async Task A_member_marked_to_be_skipped_is_not_read() =>
		await Validate(new SkippedDto());

	/// <summary>Counts how often the walk compares objects to decide whether it saw one before.</summary>
	private sealed class Comparisons
	{
		public long Count;
	}

	/// <summary>An object that counts how often it is compared, with a distinct hash per id.</summary>
	private sealed class Counted(int id, Comparisons comparisons)
	{
		public int Id { get; } = id;

		public override bool Equals(object? obj)
		{
			comparisons.Count++;

			return obj is Counted other && other.Id == Id;
		}

		public override int GetHashCode() => Id;
	}

	/// <summary>A request holding counted objects.</summary>
	private sealed class CountedDto : Dto
	{
		public List<Counted> Items { get; set; } = [];
	}

	/// <summary>Telling whether an object was seen costs no comparison with the objects before it: failed before the change with 12,497,500 comparisons for 5,000 objects.</summary>
	[TestMethod]
	public async Task Deciding_whether_an_object_was_seen_does_not_compare_it_with_every_other()
	{
		const int count = 5_000;
		var comparisons = new Comparisons();
		var dto = new CountedDto { Items = [.. Enumerable.Range(0, count).Select(f => new Counted(f, comparisons))] };

		await Validate(dto);

		/*
		 * Distinct objects with distinct hash codes need no comparison at all to be told apart. A list searched item by
		 * item makes about count * count / 2 of them (12.5 million here).
		 */
		Assert.IsTrue(comparisons.Count <= count, $"{comparisons.Count} comparisons for {count} objects.");
	}

	/// <summary>A record that holds itself: hashing it by value would never end.</summary>
	private sealed record Loop
	{
		[Required]
		public string? Name { get; set; } = "loop";

		public Loop? Self { get; set; }
	}

	/// <summary>A request holding a self-holding record.</summary>
	private sealed class LoopDto : Dto
	{
		public Loop? Loop { get; set; }
	}

	/// <summary>A record that holds itself is walked once; hashing it by value would never end.</summary>
	[TestMethod]
	public async Task A_record_that_holds_itself_ends()
	{
		var loop = new Loop();

		loop.Self = loop;

		await Validate(new LoopDto { Loop = loop });
	}

	/// <summary>Two records that hand back new equal copies of each other, a chain with a period of two.</summary>
	private sealed record Ping
	{
		[Required]
		public string? Name { get; init; } = "ping";

		public Pong Pong => new();
	}

	/// <summary>The other half of the ping-pong pair.</summary>
	private sealed record Pong
	{
		public Ping Ping => new();
	}

	/// <summary>A request holding a ping.</summary>
	private sealed class PingDto : Dto
	{
		public Ping? Ping { get; set; }
	}

	/// <summary>Records handing back new copies of each other end once a copy equals an object above it.</summary>
	[TestMethod]
	public async Task Copies_handed_back_in_turn_end() =>
		await Validate(new PingDto { Ping = new Ping() });

	/// <summary>Equal records are each walked: an invalid one after a valid equal-looking one is not let through.</summary>
	private sealed class EchoesDto : Dto
	{
		public List<Echo> Echoes { get; set; } = [];
	}

	/// <summary>Equal-looking items are each validated, so an invalid one among them still fails the request.</summary>
	[TestMethod]
	public async Task Equal_items_in_a_collection_are_all_valid_or_the_request_fails()
	{
		await Validate(new EchoesDto { Echoes = [new(), new(), new()] });

		await Assert.ThrowsExceptionAsync<ValidationException>(() => Validate(new EchoesDto { Echoes = [new(), new() { Name = null }] }));
	}

	/// <summary>Many distinct records, each with value equality: the walk stays linear.</summary>
	private sealed record Row(int Id)
	{
		[Required]
		public string? Name { get; init; } = "row";
	}

	/// <summary>A request holding many records.</summary>
	private sealed class RowsDto : Dto
	{
		public List<Row> Rows { get; set; } = [];
	}

	/// <summary>100,000 distinct records are walked within the timeout: before the change this took over 10 s.</summary>
	[TestMethod]
	[Timeout(10_000)]
	public async Task A_large_collection_of_records_is_walked_in_time() =>
		await Validate(new RowsDto { Rows = [.. Enumerable.Range(0, 100_000).Select(f => new Row(f))] });
}
