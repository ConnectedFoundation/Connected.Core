using System.Runtime.CompilerServices;

namespace Connected.Services;

/// <summary>
/// Remembers which DTOs were bound from a caller's request, so validation can hold them to what only a caller can get
/// wrong.
/// </summary>
/// <remarks>
/// <para>
/// A DTO's <c>required</c> members are checked by the compiler where code builds it with an initializer, but not where a
/// binder fills it from a request: a caller can leave a member out, and the operation then fails later with a null
/// reference that names nothing. Validation refuses such a request with the member's name, but only for DTOs a binder
/// marked here: a DTO built by code through <see cref="DtoFactory"/> is the code's own business and is not refused for
/// what it leaves unset, so no internal call starts failing because of this check.
/// </para>
/// <para>
/// The mark is held weakly: it lives exactly as long as the DTO and costs nothing once the request is done.
/// </para>
/// </remarks>
public static class RequestDtos
{
	/// <summary>
	/// The DTOs bound from a request. A <see cref="ConditionalWeakTable{TKey, TValue}"/> compares keys by reference and
	/// never keeps them alive.
	/// </summary>
	private static readonly ConditionalWeakTable<object, object> Bound = [];

	/// <summary>
	/// Marks <paramref name="dto"/> as bound from a caller's request.
	/// </summary>
	/// <param name="dto">The DTO a binder filled; null is ignored.</param>
	public static void MarkBound(object? dto)
	{
		if (dto is not null)
			Bound.AddOrUpdate(dto, Bound);
	}

	/// <summary>
	/// Whether <paramref name="dto"/> was bound from a caller's request.
	/// </summary>
	/// <param name="dto">The DTO; null is never bound.</param>
	/// <returns>True when a binder marked this very object.</returns>
	public static bool IsBound(object? dto) => dto is not null && Bound.TryGetValue(dto, out _);
}
