namespace Connected.Annotations;
/// <summary>
/// Specifies an explicit ordinal for ordering entities or properties.
/// </summary>
[AttributeUsage(AttributeTargets.Property | AttributeTargets.Class)]
public sealed class OrdinalAttribute
	: Attribute
{
	/// <summary>
	/// Initializes the attribute with a specific ordinal value.
	/// </summary>
	/// <param name="ordinal">Ordering integer.</param>
	public OrdinalAttribute(int ordinal) => Ordinal = ordinal;
	/// <summary>
	/// Gets the assigned ordinal value.
	/// </summary>
	public int Ordinal { get; }
}
