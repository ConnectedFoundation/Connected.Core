namespace Connected.Annotations;

[AttributeUsage(AttributeTargets.Property)]
public sealed class MergeableAttribute(bool isMergeable = false)
	: Attribute
{
	public bool IsMergeable { get; } = isMergeable;
}
