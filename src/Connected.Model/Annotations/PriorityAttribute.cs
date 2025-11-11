namespace Connected.Annotations;
/// <summary>
/// Specifies a priority value used to order components.
/// </summary>
[AttributeUsage(AttributeTargets.Property | AttributeTargets.Class)]
public sealed class PriorityAttribute
	: Attribute
{
	/// <summary>
	/// Initializes the attribute with a priority value.
	/// </summary>
	/// <param name="priority">Priority integer where higher/lower significance is consumer-defined.</param>
	public PriorityAttribute(int priority)
	{
		/*
		 * Persist the priority to allow sorting/selection of components.
		 */
		Priority = priority;
	}
	/// <summary>
	/// Gets the configured priority value.
	/// </summary>
	public int Priority { get; }
}