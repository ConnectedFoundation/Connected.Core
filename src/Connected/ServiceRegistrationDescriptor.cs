using Connected.Annotations;

namespace Connected;
/// <summary>
/// Describes a pending service registration including its lifetime scope, priority, and implementation type.
/// </summary>
/// <remarks>
/// Used internally by the dependency registration system to defer service registration until all candidates
/// are discovered, allowing priority-based selection when multiple implementations exist for the same interface.
/// </remarks>
internal sealed class ServiceRegistrationDescriptor(ServiceRegistrationScope scope, int priority, Type type)
{
	/// <summary>
	/// Gets or sets the priority used to resolve multiple implementations of the same interface.
	/// </summary>
	/// <remarks>
	/// Higher priority values are selected when multiple implementations are registered for the same service interface.
	/// </remarks>
	public int Priority { get; set; } = priority;
	/// <summary>
	/// Gets the lifetime scope for the service registration.
	/// </summary>
	public ServiceRegistrationScope Scope { get; } = scope;
	/// <summary>
	/// Gets the implementation type to register.
	/// </summary>
	public Type Type { get; } = type;
}
