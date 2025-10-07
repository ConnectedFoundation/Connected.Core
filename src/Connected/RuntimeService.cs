using Connected.Runtime;
using Microsoft.Extensions.Configuration;
using System.Collections.Immutable;
using System.Reflection;

namespace Connected;
/*
 * This class should not inherit from Service because the base class uses Middleware service
 * which by requires this service by default which would cause a circular reference.
 */
internal sealed class RuntimeService(IConfiguration configuration) : IRuntimeService
{
	public async Task<IImmutableList<Assembly>> QueryMicroServices()
	{
		return await Task.FromResult(MicroServices.All);
	}

	public async Task<IImmutableList<Type>> QueryMiddlewares()
	{
		return await Task.FromResult(configuration.QueryMiddlewares());
	}

	public async Task<IImmutableList<IStartup>> QueryStartups()
	{
		return await Task.FromResult(MicroServices.Startups);
	}

	public Task<IImmutableList<Assembly>> QueryUpdatedMicroServices()
	{
		throw new NotImplementedException();
	}

	public async Task<Connectivity> SelectConnectivity()
	{
		return await Task.FromResult(configuration.GetValue("connectivity", Connectivity.Online));
	}

	public async Task<Optimization> SelectOptimization()
	{
		return await Task.FromResult(configuration.GetValue("optimization", Optimization.Release));
	}

	public async Task<Platform> SelectPlatform()
	{
		return await Task.FromResult(configuration.GetValue("platform", Platform.Cloud));
	}

	public async Task<StartOptions> SelectStartOptions()
	{
		return await Task.FromResult(configuration.GetValue("startOptions", StartOptions.None));
	}
}
