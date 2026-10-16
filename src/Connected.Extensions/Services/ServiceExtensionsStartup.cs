using Connected.Annotations;
using Connected.Runtime;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System.Collections.Concurrent;

namespace Connected.Services;

[Priority(60)]
public sealed class ServiceExtensionsStartup : Startup
{
	/*
	 * Providers built from the service collection before the application's own provider exists. They used to
	 * be left undisposed, so any singleton they created (a console logger thread, for one) lived forever; they are
	 * now disposed when the application stops, which also lets a nested application's load context unload.
	 */
	private static readonly ConcurrentBag<ServiceProvider> _bootstrapProviders = [];

	public static IServiceProvider Services { get; private set; } = default!;
	public static IServiceCollection ServicesCollection { get; private set; } = default!;

	/// <summary>
	/// Builds a provider from the service collection for use before the application's own provider exists. It is
	/// disposed when the application stops.
	/// </summary>
	/// <returns>The provider.</returns>
	public static ServiceProvider BuildBootstrapProvider()
	{
		var provider = ServicesCollection.BuildServiceProvider(false);

		_bootstrapProviders.Add(provider);

		return provider;
	}

	protected override void OnConfigureServices(IServiceCollection services)
	{
		ServicesCollection = services;

		base.OnConfigureServices(services);
	}

	protected override async Task OnInitialize()
	{
		if (ServiceProvider is not null)
		{
			Services = ServiceProvider;

			ServiceProvider.GetService<IHostApplicationLifetime>()?.ApplicationStopped.Register(DisposeBootstrapProviders);
		}

		await base.OnInitialize();
	}

	private static void DisposeBootstrapProviders()
	{
		while (_bootstrapProviders.TryTake(out var provider))
		{
			try
			{
				provider.DisposeAsync().AsTask().GetAwaiter().GetResult();
			}
			catch
			{
				/*
				 * A provider that fails to dispose must not stop the others from being disposed during shutdown.
				 */
			}
		}
	}
}
