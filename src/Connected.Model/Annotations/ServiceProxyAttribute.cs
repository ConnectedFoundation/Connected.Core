namespace Connected.Annotations;
/// <summary>
/// Marks a class as a proxy wrapper for the specified service type <typeparamref name="TService"/>.
/// </summary>
/// <typeparam name="TService">The underlying service interface or class the proxy represents.</typeparam>
[AttributeUsage(AttributeTargets.Class)]
public sealed class ServiceProxyAttribute<TService>
	: Attribute
{
	/*
	 * Marker attribute with no state. Used by service discovery or generation logic to locate
	 * proxy implementations targeting a particular service type so tooling can wire or emit
	 * forwarding logic automatically.
	 */
}
