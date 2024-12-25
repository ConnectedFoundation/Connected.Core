namespace Connected.Annotations;

[AttributeUsage(AttributeTargets.Property | AttributeTargets.Class)]
public sealed class OrdinalAttribute : Attribute
{
	public OrdinalAttribute(int ordinal) => Ordinal = ordinal;
	public int Ordinal { get; }
}
