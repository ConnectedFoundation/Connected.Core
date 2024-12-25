namespace Connected.Services.Validation;

internal class ValidationServiceProvider(IServiceProvider services) : IServiceProvider
{
	public object? GetService(Type serviceType)
	{
		return services.GetService(serviceType);
	}
}
