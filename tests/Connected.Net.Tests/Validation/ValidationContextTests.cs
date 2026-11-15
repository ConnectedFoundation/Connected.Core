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

	private sealed class Item
	{
		[Required]
		public string? Name { get; set; } = "item";
	}

	private sealed class ItemsDto : Dto
	{
		public List<Item> Items { get; set; } = [];
	}

	[TestMethod]
	public async Task A_graph_of_valid_items_passes() =>
		await Validate(new ItemsDto { Items = [new(), new(), new()] });

	[TestMethod]
	public async Task An_invalid_item_deep_in_a_collection_fails()
	{
		var ex = await Assert.ThrowsExceptionAsync<ValidationException>(() => Validate(new ItemsDto { Items = [new(), new() { Name = null }] }));

		StringAssert.Contains(ex.Message, "name");
	}

	private sealed class Node
	{
		[Required]
		public string? Name { get; set; } = "node";

		public Node? Next { get; set; }
	}

	private sealed class NodeDto : Dto
	{
		public Node? Head { get; set; }
	}

	[TestMethod]
	public async Task A_reference_cycle_ends()
	{
		var a = new Node();
		var b = new Node { Next = a };

		a.Next = b;

		await Validate(new NodeDto { Head = a });
	}

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

	private sealed class EchoDto : Dto
	{
		public Echo? Echo { get; set; }
	}

	[TestMethod]
	public async Task A_member_that_returns_an_equal_copy_ends() =>
		await Validate(new EchoDto { Echo = new Echo() });

	private sealed class Unreadable
	{
		public string Value => throw new InvalidOperationException("read");
	}

	private sealed class SkippedDto : Dto
	{
		[SkipValidation]
		public Unreadable Skipped { get; set; } = new();
	}

	[TestMethod]
	public async Task A_member_marked_to_be_skipped_is_not_read() =>
		await Validate(new SkippedDto());

	/// <summary>Counts how often the walk compares objects to decide whether it saw one before.</summary>
	private sealed class Comparisons
	{
		public long Count;
	}

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

	private sealed class CountedDto : Dto
	{
		public List<Counted> Items { get; set; } = [];
	}

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

	[TestMethod]
	public async Task Equal_objects_are_still_walked_once()
	{
		var comparisons = new Comparisons();
		var dto = new CountedDto { Items = [new(1, comparisons), new(1, comparisons), new(2, comparisons)] };

		await Validate(dto);

		Assert.IsTrue(comparisons.Count >= 1, "The second equal object was not compared with the first.");
	}
}
