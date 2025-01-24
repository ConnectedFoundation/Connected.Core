namespace Connected.Annotations;

[Flags]
public enum ServiceVisibilityScope
{
	InProcess = 0,
	InternalNetwork = 1,
	//ExternalGrpcClients = 2,
	All = 3
}

[AttributeUsage(AttributeTargets.Class)]
public sealed class ServiceVisibilityAttribute(ServiceVisibilityScope scope = ServiceVisibilityScope.InProcess) : Attribute
{
	public ServiceVisibilityScope Scope { get; } = scope;
}
