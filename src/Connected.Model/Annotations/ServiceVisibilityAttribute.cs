namespace Connected.Annotations;
/// <summary>
/// Defines visibility scopes that control how a service can be accessed.
/// </summary>
[Flags]
public enum ServiceVisibilityScope
{
	/// <summary>
	/// Service is visible only within the current process.
	/// </summary>
	InProcess = 0,
	/// <summary>
	/// Service is visible only within the internal network.
	/// </summary>
	InternalNetwork = 1,
	/// <summary>
	/// Service is visible to all supported scopes.
	/// </summary>
	All = 3
}
/// <summary>
/// Attribute specifying the <see cref="ServiceVisibilityScope"/> for a service type.
/// </summary>
/// <param name="scope">The visibility scope for the decorated service.</param>
[AttributeUsage(AttributeTargets.Class)]
public sealed class ServiceVisibilityAttribute(ServiceVisibilityScope scope = ServiceVisibilityScope.InProcess)
	: Attribute
{
	/*
	 * Marker attribute that conveys intended exposure of a service to hosting and routing layers.
	 * The scope guides how the service is advertised or bound to endpoints.
	 */
	/// <summary>
	/// Gets the configured visibility scope.
	/// </summary>
	public ServiceVisibilityScope Scope { get; } = scope;
}
