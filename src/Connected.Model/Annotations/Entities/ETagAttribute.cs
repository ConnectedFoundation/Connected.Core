namespace Connected.Annotations.Entities;
/// <summary>
/// Specifies that a property should be regarded as an ETag field, sometimes called timestamp.
/// </summary>
/// <remarks>
/// This is a markup attribute.
/// </remarks>
[AttributeUsage(AttributeTargets.Property)]
public sealed class ETagAttribute
	: Attribute
{
	/*
	 * Marker attribute with no state: used by concurrency logic to identify properties
	 * whose values act as optimistic concurrency tokens (ETags).
	 */
}
