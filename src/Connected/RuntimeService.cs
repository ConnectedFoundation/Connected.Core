using Connected.Runtime;
using Microsoft.Extensions.Configuration;
using System.Collections.Immutable;
using System.Reflection;

namespace Connected;
/// <summary>
/// Provides runtime information about the application including dependencies, middleware, startups, and configuration options.
/// </summary>
/// <remarks>
/// This class should not inherit from Service because the base class uses Middleware service
/// which by requires this service by default which would cause a circular reference.
/// </remarks>
internal sealed class RuntimeService(IConfiguration configuration)
	: IRuntimeService
{
	/// <summary>
	/// Asynchronously queries all registered dependency assemblies.
	/// </summary>
	/// <returns>An immutable list of registered assemblies.</returns>
	/// <inheritdoc/>
	public async Task<IImmutableList<Assembly>> QueryDependencies()
	{
		return await Task.FromResult(Dependencies.All);
	}
	/// <summary>
	/// Asynchronously queries all registered middleware types.
	/// </summary>
	/// <returns>An immutable list of middleware types.</returns>
	/// <inheritdoc/>
	public async Task<IImmutableList<Type>> QueryMiddlewares()
	{
		return await Task.FromResult(CoreUtils.QueryMiddlewares());
	}
	/// <summary>
	/// Asynchronously queries all discovered startup components.
	/// </summary>
	/// <returns>An immutable list of startup instances.</returns>
	/// <inheritdoc/>
	public async Task<IImmutableList<IStartup>> QueryStartups()
	{
		return await Task.FromResult(Dependencies.Startups);
	}
	/// <summary>
	/// Asynchronously queries assemblies that have been updated since the last query.
	/// </summary>
	/// <returns>An immutable list of updated assemblies.</returns>
	/// <exception cref="NotImplementedException">This method is not yet implemented.</exception>
	/// <inheritdoc/>
	public Task<IImmutableList<Assembly>> QueryUpdatedDependencies()
	{
		throw new NotImplementedException();
	}
	/// <summary>
	/// Asynchronously selects the connectivity mode from configuration.
	/// </summary>
	/// <returns>The configured connectivity mode, defaulting to <see cref="Connectivity.Online"/>.</returns>
	/// <inheritdoc/>
	public async Task<Connectivity> SelectConnectivity()
	{
		return await Task.FromResult(configuration.GetValue("connectivity", Connectivity.Online));
	}
	/// <summary>
	/// Asynchronously selects the optimization mode from configuration.
	/// </summary>
	/// <returns>The configured optimization mode, defaulting to <see cref="Optimization.Release"/>.</returns>
	/// <inheritdoc/>
	public async Task<Optimization> SelectOptimization()
	{
		return await Task.FromResult(configuration.GetValue("optimization", Optimization.Release));
	}
	/// <summary>
	/// Asynchronously selects the platform mode from configuration.
	/// </summary>
	/// <returns>The configured platform mode, defaulting to <see cref="Platform.Cloud"/>.</returns>
	/// <inheritdoc/>
	public async Task<Platform> SelectPlatform()
	{
		return await Task.FromResult(configuration.GetValue("platform", Platform.Cloud));
	}
	/// <summary>
	/// Asynchronously selects the startup options from configuration.
	/// </summary>
	/// <returns>The configured startup options, defaulting to <see cref="StartOptions.None"/>.</returns>
	/// <inheritdoc/>
	public async Task<StartOptions> SelectStartOptions()
	{
		return await Task.FromResult(configuration.GetValue("startOptions", StartOptions.None));
	}
}
