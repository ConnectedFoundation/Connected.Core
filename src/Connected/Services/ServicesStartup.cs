using Connected.Services.Validation;
using Microsoft.Extensions.DependencyInjection;

namespace Connected.Services;
public sealed class ServicesStartup : Runtime.Startup
{
	protected override void OnConfigureServices(IServiceCollection services)
	{
		services.AddScoped<ICancellationContext, CancellationContext>();
		services.AddScoped<IValidationContext, ValidationContext>();
	}
}
