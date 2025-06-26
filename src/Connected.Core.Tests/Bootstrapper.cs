using Connected.Identities.Users;
using Connected.Runtime;
using Microsoft.Extensions.DependencyInjection;

namespace Connected.Core.Tests;

internal sealed class Bootstrapper : Startup
{
	protected override void OnConfigureServices(IServiceCollection services)
	{
		services.AddScoped<InsertUser>();
		services.AddScoped<UserTests>();
	}
}
