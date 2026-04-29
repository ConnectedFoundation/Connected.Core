using System.Collections.Immutable;

namespace Connected.Annotations;
/// <summary>
/// Associates a stable identifier with a middleware type to support discovery and referencing.
/// </summary>
[AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
public sealed class MiddlewareIdAttribute(string id, string? description = null, string? tags = null)
	: Attribute
{
	/*
	 * Persist the identifier to allow lookup or filtering of middleware components by id.
	 */
	/// <summary>
	/// Gets the middleware identifier.
	/// </summary>
	public string Id { get; } = id;

	public string? Description { get; } = description;
	public string? Tags { get; } = tags;

	public bool HasTag(string tag)
	{
		if (Tags is null)
			return false;

		var tokens = SplitTags(Tags);

		return tokens.Contains(tag, StringComparer.OrdinalIgnoreCase);
	}

	public static ImmutableArray<string> SplitTags(string value)
	{
		return [.. value.Split([',', ';'], StringSplitOptions.RemoveEmptyEntries)];
	}

	public ImmutableArray<string> SplitTags()
	{
		if (Tags is not { Length: > 0 })
			return [];

		return SplitTags(Tags);
	}
}
