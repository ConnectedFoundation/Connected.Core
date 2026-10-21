using Connected.Annotations;
using Connected.Runtime;
using Microsoft.Extensions.DependencyInjection;

namespace Connected.Services;

[Priority(60)]
public sealed class ServiceExtensionsStartup : Startup
{
	/*
	 * One provider serves everything resolved before the application's own provider exists. It is rebuilt only when the
	 * service collection differs from the one it was built from, registration by registration, so it always sees what
	 * is registered so far. What it hands out (scopes, singletons, DTOs) may be kept for the whole run, so no bootstrap
	 * provider is disposed while the application runs, a replaced one included; all of them are disposed once, when
	 * the run ends.
	 */
	private static readonly Lock _bootstrapLock = new();
	private static readonly List<ServiceProvider> _bootstrapProviders = [];
	private static ServiceProvider? _bootstrapProvider;
	private static ServiceDescriptor[] _bootstrapSnapshot = [];
	private static volatile IServiceProvider? _services;

	public static IServiceProvider Services
	{
		get => _services!;
		private set => _services = value;
	}

	public static IServiceCollection ServicesCollection { get; private set; } = default!;

	/// <summary>
	/// Gets the provider for use before the application's own provider exists, built from the service collection as it
	/// is now, or the application's own provider once it does. Callers never dispose it; what it hands out stays valid
	/// until the application stops.
	/// </summary>
	/// <returns>The provider.</returns>
	public static IServiceProvider BootstrapProvider()
	{
		lock (_bootstrapLock)
		{
			if (_services is { } services)
				return services;

			var collection = ServicesCollection ?? throw new InvalidOperationException("The application's services are not configured.");

			if (_bootstrapProvider is null || !Unchanged(collection))
			{
				_bootstrapProvider = collection.BuildServiceProvider(false);
				_bootstrapSnapshot = [.. collection];
				_bootstrapProviders.Add(_bootstrapProvider);
			}

			return _bootstrapProvider;
		}
	}

	private static bool Unchanged(IServiceCollection collection)
	{
		if (collection.Count != _bootstrapSnapshot.Length)
			return false;

		for (var i = 0; i < _bootstrapSnapshot.Length; i++)
		{
			if (!ReferenceEquals(collection[i], _bootstrapSnapshot[i]))
				return false;
		}

		return true;
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
			/*
			 * From here on no bootstrap provider is handed out; the ones already built stay alive until the run ends.
			 */
			lock (_bootstrapLock)
				Services = ServiceProvider;
		}

		await base.OnInitialize();
	}

	/// <summary>
	/// Clears what one run of the application left here once it has stopped or failed to start: the bootstrap providers
	/// are disposed and <see cref="Services"/> and <see cref="ServicesCollection"/> are unset, so a later start in the
	/// same process builds and uses its own instead of the disposed ones.
	/// </summary>
	/// <remarks>
	/// Called after the application's own provider is disposed, since its services may hold what a bootstrap provider
	/// handed out, and before the cache shuts down, since what a bootstrap provider disposes may still use it.
	/// </remarks>
	public static async Task Reset()
	{
		/*
		 * Both unset together, so nothing can build another bootstrap provider while these are disposed.
		 */
		lock (_bootstrapLock)
		{
			Services = null!;
			ServicesCollection = null!;
		}

		await DisposeBootstrapProviders();
	}

	private static async Task DisposeBootstrapProviders()
	{
		List<ServiceProvider> providers;

		lock (_bootstrapLock)
		{
			providers = [.. _bootstrapProviders];

			_bootstrapProviders.Clear();
			_bootstrapProvider = null;
			_bootstrapSnapshot = [];
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
