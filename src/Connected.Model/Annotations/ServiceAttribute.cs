namespace Connected.Annotations;
/// <summary>
/// Specifies how a Service is registered in the Dependency Injection container.
/// </summary>
public enum ServiceRegistrationScope
{
	/// <summary>
	/// Only one instance of the service will be shared across the entire process.
	/// </summary>
	Singleton = 1,
	/// <summary>
	/// A service instance will be created inside each IContext, which represents a scope.
	/// </summary>
	Scoped = 2,
	/// <summary>
	/// A new instance will be created for every request in the dependency Injection container.
	/// </summary>
	Transient = 3
}
/// <summary>
/// Specifies that a component should be treated as a service.
/// </summary>
[AttributeUsage(AttributeTargets.Interface | AttributeTargets.Class)]
public sealed class ServiceAttribute
	: Attribute
{
	/// <summary>
	/// Creates a new instance of the ServiceAttribute class.
	/// </summary>
	public ServiceAttribute()
	{
		/*
		 * Default scope remains scoped unless explicitly overridden.
		 */
	}
	/// <summary>
	/// Creates a new instance of the MiddlewareAttribute class with a specific scope.
	/// </summary>
	/// <param name="scope">The scope used by a Connected when registering a service 
	/// in the Dependency Injection Container.</param>
	public ServiceAttribute(ServiceRegistrationScope scope)
	{
		/*
		 * Persist the desired scope for DI registration.
		 */
		Scope = scope;
	}
	/// <summary>
	/// Gets the scope that will be used when registering service in the Dependency Injection container. 
	/// </summary>
	public ServiceRegistrationScope Scope { get; } = ServiceRegistrationScope.Scoped;
}