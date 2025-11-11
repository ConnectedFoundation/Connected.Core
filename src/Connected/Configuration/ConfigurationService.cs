using System;
using Connected.Configuration.Authentication;
using Microsoft.Extensions.Configuration;

namespace Connected.Configuration;

/// <summary>
/// Default implementation of <see cref="IConfigurationService"/> that reads
/// configuration sections from an <see cref="IConfiguration"/> instance.
/// </summary>
/// <remarks>
/// This implementation is intended to be registered as a singleton. It resolves
/// and caches typed configuration objects for authentication, storage and routing
/// so callers can depend on a stable, thread-safe configuration view.
/// 
/// See also the configuration contract defined by the <see cref="IConfigurationService"/>
/// interface (source: <c>Connected.Model/Configuration/IConfigurationService.cs</c>).
/// </remarks>
/// <seealso cref="IConfigurationService" />
internal sealed class ConfigurationService : IConfigurationService
{
	private readonly IConfiguration _configuration;

	/// <summary>
	/// Initializes a new instance of the <see cref="ConfigurationService"/> class.
	/// </summary>
	/// <param name="configuration">The root application configuration used to bind typed configuration sections.</param>
	/// <exception cref="ArgumentNullException">Thrown when <paramref name="configuration"/> is <c>null</c>.</exception>
	public ConfigurationService(IConfiguration configuration)
	{
		_configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));

		// Bind and cache configuration sections at construction time. Implementations
		// are expected to be singleton and safe for concurrent use.
		Authentication = new AuthenticationConfiguration(_configuration.GetSection("authentication"));
		Storage = new StorageConfiguration(_configuration.GetSection("storage"));
		Routing = new RoutingConfiguration(_configuration.GetSection("routing"));
	}

	/// <summary>
	/// Gets the authentication-related configuration values such as JWT settings.
	/// </summary>
	/// <remarks>
	/// See <see cref="IConfigurationService.Authentication"/> for the configuration contract.
	/// </remarks>
	public IAuthenticationConfiguration Authentication { get; }

	/// <summary>
	/// Gets configuration values related to storage and database connections.
	/// </summary>
	/// <remarks>
	/// See <see cref="IConfigurationService.Storage"/> for the configuration contract.
	/// </remarks>
	public IStorageConfiguration Storage { get; }

	/// <summary>
	/// Gets routing configuration used by network and service discovery components.
	/// </summary>
	/// <remarks>
	/// See <see cref="IConfigurationService.Routing"/> for the configuration contract.
	/// </remarks>
	public IRoutingConfiguration Routing { get; }
}
