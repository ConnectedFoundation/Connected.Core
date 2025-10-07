namespace Connected.Annotations;

[AttributeUsage(AttributeTargets.Class)]
public sealed class ServiceProxyAttribute<TService> : Attribute
{
}
