namespace Connected.Annotations;
/// <summary>
/// Specifies how the service will be registered on the startup.
/// </summary>
public enum ServiceRegistrationMode
{
	/// <summary>
	/// Service will be automatically registered in the Dependency Injection container.
	/// </summary>
	Auto = 1,
	/// <summary>
	/// Service won't be registered in the Dependency Injection container automatically. The
	/// developer will take care of the registration process.
	/// </summary>
	Manual = 2
}
/// <summary>
/// Specifies how the service is registered on startup.
/// </summary>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
public sealed class ServiceRegistrationAttribute : Attribute
{
	/// <summary>
	/// Creates a new instance of the ServiceRegistrationAttribute class.
	/// </summary>
	/// <param name="mode">A registration mode to instruct the Connected wether 
	/// to perform automatic registration or not.</param>
	public ServiceRegistrationAttribute(ServiceRegistrationMode mode)
	{
		Mode = mode;
	}
	/// <summary>
	/// Gets a registration mode for the service.
	/// </summary>
	public ServiceRegistrationMode Mode { get; }
}
