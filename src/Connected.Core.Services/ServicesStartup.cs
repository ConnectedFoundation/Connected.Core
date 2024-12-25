using Connected.Runtime;
using Connected.Services.Validation;
using Microsoft.Extensions.DependencyInjection;

namespace Connected.Services;
public sealed class ServicesStartup : Startup
{
	protected override void OnConfigureServices(IServiceCollection services)
	{
		services.AddScoped(typeof(ICancellationContext), typeof(CancellationContext));
		services.AddScoped(typeof(IValidationContext), typeof(ValidationContext));
	}
}
