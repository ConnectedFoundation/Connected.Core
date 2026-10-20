using Connected.Annotations;
using Connected.Runtime;
using Microsoft.Extensions.DependencyInjection;
using System.Collections.Concurrent;

namespace Connected.Services;

[Priority(60)]
public sealed class ServiceExtensionsStartup : Startup
{
	/*
	 * Providers built from the service collection before the application's own provider exists whose results outlive
	 * the call (scopes, singletons). They used to be left undisposed, so any singleton they created (a console logger
	 * thread, for one) lived forever; they are now disposed as soon as the application's own provider is in place,
	 * since nothing builds one after that, or when a start fails before it. Providers that only resolve a transient
	 * DTO are disposed straight away.
	 */
	private static readonly ConcurrentBag<ServiceProvider> _bootstrapProviders = [];

	public static IServiceProvider Services { get; private set; } = default!;
	public static IServiceCollection ServicesCollection { get; private set; } = default!;

	/// <summary>
	/// Builds a provider from the service collection for use before the application's own provider exists. It is
	/// disposed once the application's own provider is in place.
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

			DisposeBootstrapProviders();
		}

		await base.OnInitialize();
	}

	/// <summary>
	/// Disposes every provider built by <see cref="BuildBootstrapProvider"/>. Called once the application's own provider
	/// is in place, and again once the application has stopped or failed to start.
	/// </summary>
	public static void DisposeBootstrapProviders()
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
