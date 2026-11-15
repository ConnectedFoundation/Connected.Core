using Connected.Services;
using Microsoft.Extensions.DependencyInjection;
using System.ComponentModel.DataAnnotations;
using ValidationContext = Connected.Services.Validation.ValidationContext;

namespace Connected.Validation.Tests;

/// <summary>
/// A request that leaves out a member its DTO declares <c>required</c> is refused before the service runs, naming the member.
/// C# <c>required</c> is a compile-time contract only: a request bound from JSON can arrive with the member null, and the
/// operation then failed later with a null reference that named nothing.
/// </summary>
[TestClass]
public sealed class RequiredMembersTests
{
	private static Task Validate(IDto dto)
	{
		var context = new ValidationContext(null!, new ServiceCollection().BuildServiceProvider());

		return context.Validate(null!, dto);
	}

	/// <summary>A request with required members of every kind the check must tell apart.</summary>
	private class OrderLineDto : Dto
	{
		public required string Order { get; set; }

		public required string Category { get; set; }

		/// <summary>Required, but null is a value the contract allows.</summary>
		public required string? Lot { get; set; }

		/// <summary>Not required: may be left out.</summary>
		public string? Unit { get; set; }

		/// <summary>A required value type: leaving it out cannot be told from its default, so it is not checked.</summary>
		public required double Quantity { get; set; }

		/// <summary>A required nullable value type: null is allowed by its type.</summary>
		public required DateTimeOffset? At { get; set; }
	}

	/// <summary>A derived request: required members declared on the base are still required.</summary>
	private sealed class InsertOrderLineDto : OrderLineDto
	{
	}

	/// <summary>
	/// Builds a DTO as the factory does: through a constructor that does not know about <c>required</c>. The DTO is not
	/// marked as a caller's request.
	/// </summary>
	private static T Built<T>(Action<T> fill)
		where T : class
	{
		var dto = (T)Activator.CreateInstance(typeof(T), nonPublic: true)!;

		fill(dto);

		return dto;
	}

	/// <summary>Builds a request as the REST binder does, and marks it as a caller's request as the binder does.</summary>
	private static T Bound<T>(Action<T> fill)
		where T : class
	{
		var dto = Built(fill);

		RequestDtos.MarkBound(dto);

		return dto;
	}

	/// <summary>
	/// A DTO built by code, not bound from a request, is not refused for a required member it leaves unset: the check is
	/// for callers, and no internal call may start failing because of it.
	/// </summary>
	[TestMethod]
	public async Task A_dto_built_by_code_is_not_held_to_required() =>
		await Validate(Built<OrderLineDto>(f => f.Order = "run-1"));

	/// <summary>Every non-nullable required member set: the request passes.</summary>
	[TestMethod]
	public async Task A_request_with_every_required_member_passes() =>
		await Validate(Bound<OrderLineDto>(f =>
		{
			f.Order = "run-1";
			f.Category = "food";
		}));

	/// <summary>A non-nullable required member left out is refused, naming it as the request spells it.</summary>
	[TestMethod]
	public async Task A_missing_required_member_is_refused_and_named()
	{
		var ex = await Assert.ThrowsExceptionAsync<ValidationException>(() => Validate(Bound<OrderLineDto>(f => f.Order = "run-1")));

		StringAssert.Contains(ex.Message, "category");
	}

	/// <summary>A required member declared on a base class is checked on the derived request.</summary>
	[TestMethod]
	public async Task A_required_member_of_a_base_class_is_checked() =>
		await Assert.ThrowsExceptionAsync<ValidationException>(() => Validate(Bound<InsertOrderLineDto>(f => f.Category = "food")));

	/// <summary>Required members whose type allows null, value types, and members that are not required may be left out.</summary>
	[TestMethod]
	public async Task Members_whose_type_allows_null_and_value_types_may_be_left_out() =>
		await Validate(Bound<OrderLineDto>(f =>
		{
			f.Order = "run-1";
			f.Category = "food";
			f.Lot = null;
			f.At = null;
		}));

	/// <summary>An empty string is a value: whether it is allowed is the operation's or an attribute's decision.</summary>
	[TestMethod]
	public async Task An_empty_string_is_not_a_missing_member() =>
		await Validate(Bound<OrderLineDto>(f =>
		{
			f.Order = "run-1";
			f.Category = string.Empty;
		}));

	/// <summary>A request nested in another request is a request too: its required members are checked.</summary>
	private sealed class BatchDto : Dto
	{
		public List<OrderLineDto> Items { get; set; } = [];
	}

	/// <summary>A nested request with a missing required member fails the whole request.</summary>
	[TestMethod]
	public async Task A_nested_request_missing_a_required_member_fails()
	{
		/*
		 * Only the outer request is marked, as the binder marks only the argument it fills: what it holds came with it.
		 */
		var ok = Built<OrderLineDto>(f => { f.Order = "run-1"; f.Category = "food"; });
		var missing = Built<OrderLineDto>(f => f.Order = "run-2");
		var batch = Bound<BatchDto>(f => f.Items = [ok, missing]);

		var ex = await Assert.ThrowsExceptionAsync<ValidationException>(() => Validate(batch));

		StringAssert.Contains(ex.Message, "category");
	}

	/// <summary>
	/// An object that is not a request (an entity or a model the server built) is not held to <c>required</c>: only what a
	/// caller sends is, so data the server already holds cannot start failing requests that carry it.
	/// </summary>
	private sealed class Entity
	{
		public required string Code { get; set; }
	}

	/// <summary>A request carrying a non-request object.</summary>
	private sealed class CarrierDto : Dto
	{
		public Entity? Entity { get; set; }
	}

	/// <summary>A required member left null on a non-request object inside a request does not fail it.</summary>
	[TestMethod]
	public async Task A_non_request_object_is_not_held_to_required() =>
		await Validate(Bound<CarrierDto>(f => f.Entity = Built<Entity>(_ => { })));
}
