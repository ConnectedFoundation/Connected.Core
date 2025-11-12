namespace Connected.Annotations;
/// <summary>
/// Specifies an explicit ordinal for ordering entities or properties.
/// </summary>
/// <remarks>
/// Initializes the attribute with a specific ordinal value.
/// </remarks>
/// <param name="ordinal">Ordering integer.</param>
[AttributeUsage(AttributeTargets.Property | AttributeTargets.Class)]
public sealed class OrdinalAttribute(int ordinal)
		: Attribute
{
	/// <summary>
	/// Gets the assigned ordinal value.
	/// </summary>
	public int Ordinal { get; } = ordinal;
}
