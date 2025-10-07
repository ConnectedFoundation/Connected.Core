using Connected.Annotations;

namespace Connected;
internal sealed class ServiceRegistrationDescriptor(ServiceRegistrationScope scope, int priority, Type type)
{
	public int Priority { get; set; } = priority;
	public ServiceRegistrationScope Scope { get; } = scope;
	public Type Type { get; } = type;
}
