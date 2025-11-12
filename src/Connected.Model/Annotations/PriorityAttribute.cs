namespace Connected.Annotations;
/// <summary>
/// Specifies a priority value used to order components.
/// </summary>
/// <remarks>
/// Initializes the attribute with a priority value.
/// </remarks>
/// <param name="priority">Priority integer where higher/lower significance is consumer-defined.</param>
[AttributeUsage(AttributeTargets.Property | AttributeTargets.Class)]
public sealed class PriorityAttribute(int priority)
		: Attribute
{
	/// <summary>
	/// Gets the configured priority value.
	/// </summary>
	public int Priority { get; } = priority;
}