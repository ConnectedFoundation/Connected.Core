using Connected.Annotations;
using Connected.Runtime;
using Microsoft.Extensions.DependencyInjection;

namespace Connected.Services;

[Priority(60)]
public sealed class ServiceExtensionsStartup : Startup
{
	/*
	 * One provider serves everything resolved before the application's own provider exists. It is rebuilt only when the
	 * service collection has changed since it was built, so it always sees what is registered so far: a registration
	 * adds at the end and a replacement removes and adds, so the count and the last registration together tell. A
	 * replaced provider is kept rather than disposed, since a scope or a singleton taken from it may still be in use;
	 * all of them are disposed once the application's own provider is in place, or when the run ends.
	 */
	private static readonly Lock _bootstrapLock = new();
	private static readonly List<ServiceProvider> _bootstrapProviders = [];
	private static ServiceProvider? _bootstrapProvider;
	private static int _bootstrapCount = -1;
	private static ServiceDescriptor? _bootstrapLast;

	public static IServiceProvider Services { get; private set; } = default!;
	public static IServiceCollection ServicesCollection { get; private set; } = default!;

	/// <summary>
	/// Gets the provider for use before the application's own provider exists, built from the service collection as it
	/// is now. It is disposed once the application's own provider is in place; callers never dispose it.
	/// </summary>
	/// <returns>The provider.</returns>
	public static ServiceProvider BootstrapProvider()
	{
		lock (_bootstrapLock)
		{
			var services = ServicesCollection;
			var last = services.Count > 0 ? services[^1] : null;

			if (_bootstrapProvider is null || _bootstrapCount != services.Count || !ReferenceEquals(_bootstrapLast, last))
			{
				_bootstrapProvider = services.BuildServiceProvider(false);
				_bootstrapCount = services.Count;
				_bootstrapLast = last;
				_bootstrapProviders.Add(_bootstrapProvider);
			}

			return _bootstrapProvider;
		}
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

			await DisposeBootstrapProviders();
		}

		await base.OnInitialize();
	}

	/// <summary>
	/// Clears what one run of the application left here once it has stopped or failed to start: the bootstrap providers
	/// are disposed and <see cref="Services"/> and <see cref="ServicesCollection"/> are unset, so a later start in the
	/// same process builds and uses its own instead of the disposed ones.
	/// </summary>
	public static async Task Reset()
	{
		await DisposeBootstrapProviders();

		Services = null!;
		ServicesCollection = null!;
	}

	private static async Task DisposeBootstrapProviders()
	{
		List<ServiceProvider> providers;

		lock (_bootstrapLock)
		{
			providers = [.. _bootstrapProviders];

			_bootstrapProviders.Clear();
			_bootstrapProvider = null;
			_bootstrapCount = -1;
			_bootstrapLast = null;
		}

		foreach (var provider in providers)
		{
			try
			{
				await provider.DisposeAsync();
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
