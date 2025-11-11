using Connected.Annotations;
using System.Collections.Immutable;
using System.Reflection;

namespace Connected.Runtime;

/// <summary>
/// Defines startup configuration options for the application.
/// </summary>
/// <remarks>
/// This flags enumeration allows multiple startup options to be specified by combining
/// individual option flags using bitwise operations. Startup options control various
/// initialization behaviors during application startup, such as database schema
/// synchronization.
/// </remarks>
[Flags]
public enum StartOptions
{
	/// <summary>
	/// Indicates no special startup options are enabled.
	/// </summary>
	None = 0,

	/// <summary>
	/// Indicates that database schemas should be synchronized during startup.
	/// </summary>
	/// <remarks>
	/// When enabled, the application will compare current database schemas with
	/// expected schemas and perform necessary migrations or updates.
	/// </remarks>
	SynchronizeSchemas = 1
}

/// <summary>
/// Defines the deployment stage of the application.
/// </summary>
/// <remarks>
/// This enumeration categorizes the environment in which the application is running,
/// allowing for stage-specific configuration, logging levels, feature flags, and
/// behaviors. Each stage typically has different requirements for debugging,
/// monitoring, and security.
/// </remarks>
public enum Stage
{
	/// <summary>
	/// Represents the development environment.
	/// </summary>
	/// <remarks>
	/// Used during active development with detailed logging, debugging tools enabled,
	/// and relaxed security constraints.
	/// </remarks>
	Development = 1,

	/// <summary>
	/// Represents the quality assurance (testing) environment.
	/// </summary>
	/// <remarks>
	/// Used for testing and validation with production-like configurations but
	/// with additional debugging capabilities and test data.
	/// </remarks>
	QualityAssurance = 2,

	/// <summary>
	/// Represents the staging (pre-production) environment.
	/// </summary>
	/// <remarks>
	/// Used as a final validation environment that closely mirrors production
	/// settings before deployment to production.
	/// </remarks>
	Staging = 3,

	/// <summary>
	/// Represents the production environment.
	/// </summary>
	/// <remarks>
	/// Used for live production deployments with optimized performance, minimal
	/// logging, and full security constraints.
	/// </remarks>
	Production = 4
}

/// <summary>
/// Defines the deployment platform type.
/// </summary>
/// <remarks>
/// This enumeration indicates whether the application is deployed on-premises or
/// in a cloud environment, which affects resource provisioning, scaling strategies,
/// and infrastructure management approaches.
/// </remarks>
public enum Platform
{
	/// <summary>
	/// Indicates the application is deployed on-premises.
	/// </summary>
	/// <remarks>
	/// The application runs on infrastructure managed directly by the organization,
	/// typically in physical or virtualized data centers.
	/// </remarks>
	OnPrem = 1,

	/// <summary>
	/// Indicates the application is deployed in a cloud environment.
	/// </summary>
	/// <remarks>
	/// The application runs on cloud infrastructure (e.g., Azure, AWS, GCP) with
	/// access to cloud-native services and elastic scaling capabilities.
	/// </remarks>
	Cloud = 2
}

/// <summary>
/// Defines the network connectivity mode of the application.
/// </summary>
/// <remarks>
/// This enumeration indicates whether the application operates with continuous network
/// connectivity or supports offline scenarios. This affects data synchronization,
/// caching strategies, and user experience patterns.
/// </remarks>
public enum Connectivity
{
	/// <summary>
	/// Indicates the application requires continuous network connectivity.
	/// </summary>
	/// <remarks>
	/// The application operates with real-time access to remote services and data sources.
	/// </remarks>
	Online = 1,

	/// <summary>
	/// Indicates the application supports offline operation.
	/// </summary>
	/// <remarks>
	/// The application can function without network connectivity using cached data
	/// and deferred synchronization mechanisms.
	/// </remarks>
	Offline = 2
}

/// <summary>
/// Defines the compilation optimization mode.
/// </summary>
/// <remarks>
/// This enumeration indicates whether the application is compiled with debugging
/// information and checks (Debug) or with optimizations for performance (Release).
/// This affects performance characteristics, diagnostics capabilities, and binary size.
/// </remarks>
public enum Optimization
{
	/// <summary>
	/// Indicates the application is compiled in debug mode.
	/// </summary>
	/// <remarks>
	/// Debug mode includes debugging symbols, additional runtime checks, and
	/// reduced optimizations to facilitate troubleshooting and development.
	/// </remarks>
	Debug = 1,

	/// <summary>
	/// Indicates the application is compiled in release mode.
	/// </summary>
	/// <remarks>
	/// Release mode applies compiler optimizations for performance, removes
	/// debugging symbols, and minimizes binary size for production deployments.
	/// </remarks>
	Release = 2
}

/// <summary>
/// Provides runtime configuration and dependency management services.
/// </summary>
/// <remarks>
/// This singleton service provides access to runtime metadata including loaded assemblies,
/// startup components, middleware configurations, and environment settings. It enables
/// dynamic discovery of dependencies, tracking of updated assemblies, and querying of
/// runtime characteristics such as platform, connectivity, and optimization settings.
/// The service plays a central role in the application's bootstrap and initialization
/// process by coordinating the loading and configuration of modular components.
/// </remarks>
[Service(ServiceRegistrationScope.Singleton)]
public interface IRuntimeService
{
	/// <summary>
	/// Asynchronously queries all loaded dependency assemblies.
	/// </summary>
	/// <returns>
	/// A task that represents the asynchronous operation. The task result contains
	/// an immutable list of all assemblies that are registered as dependencies.
	/// </returns>
	/// <remarks>
	/// This method provides access to all assemblies that the application depends on,
	/// including both direct and transitive dependencies. This information is useful
	/// for reflection-based operations, plugin discovery, and dependency analysis.
	/// </remarks>
	Task<IImmutableList<Assembly>> QueryDependencies();

	/// <summary>
	/// Asynchronously queries all registered startup components.
	/// </summary>
	/// <returns>
	/// A task that represents the asynchronous operation. The task result contains
	/// an immutable list of all startup components implementing <see cref="IStartup"/>.
	/// </returns>
	/// <remarks>
	/// Startup components handle various initialization phases including service
	/// configuration, middleware setup, and endpoint routing. This method allows
	/// for discovery and inspection of all registered startup components.
	/// </remarks>
	Task<IImmutableList<IStartup>> QueryStartups();

	/// <summary>
	/// Asynchronously queries dependency assemblies that have been updated.
	/// </summary>
	/// <returns>
	/// A task that represents the asynchronous operation. The task result contains
	/// an immutable list of assemblies that have been modified or updated.
	/// </returns>
	/// <remarks>
	/// This method is useful for hot-reload scenarios, plugin updates, or detecting
	/// changes in dependencies that may require reinitialization or reconfiguration.
	/// </remarks>
	Task<IImmutableList<Assembly>> QueryUpdatedDependencies();

	/// <summary>
	/// Asynchronously retrieves the configured startup options.
	/// </summary>
	/// <returns>
	/// A task that represents the asynchronous operation. The task result contains
	/// the <see cref="StartOptions"/> flags indicating which startup behaviors are enabled.
	/// </returns>
	/// <remarks>
	/// Startup options control various initialization behaviors such as schema
	/// synchronization. Applications can query these options to adjust their
	/// startup behavior accordingly.
	/// </remarks>
	Task<StartOptions> SelectStartOptions();

	/// <summary>
	/// Asynchronously retrieves the deployment platform configuration.
	/// </summary>
	/// <returns>
	/// A task that represents the asynchronous operation. The task result contains
	/// the <see cref="Platform"/> value indicating whether the application is running
	/// on-premises or in the cloud.
	/// </returns>
	/// <remarks>
	/// The platform setting affects infrastructure assumptions, resource provisioning,
	/// and integration with platform-specific services.
	/// </remarks>
	Task<Platform> SelectPlatform();

	/// <summary>
	/// Asynchronously retrieves the network connectivity mode.
	/// </summary>
	/// <returns>
	/// A task that represents the asynchronous operation. The task result contains
	/// the <see cref="Connectivity"/> value indicating whether the application operates
	/// online or supports offline scenarios.
	/// </returns>
	/// <remarks>
	/// The connectivity mode affects data synchronization strategies, caching behaviors,
	/// and user interface patterns for handling network availability.
	/// </remarks>
	Task<Connectivity> SelectConnectivity();

	/// <summary>
	/// Asynchronously retrieves the compilation optimization mode.
	/// </summary>
	/// <returns>
	/// A task that represents the asynchronous operation. The task result contains
	/// the <see cref="Optimization"/> value indicating whether the application was
	/// compiled in debug or release mode.
	/// </returns>
	/// <remarks>
	/// The optimization setting helps components adjust their behavior based on
	/// whether they're running in a development or production environment.
	/// </remarks>
	Task<Optimization> SelectOptimization();

	/// <summary>
	/// Asynchronously queries all registered middleware types.
	/// </summary>
	/// <returns>
	/// A task that represents the asynchronous operation. The task result contains
	/// an immutable list of all middleware types registered in the application.
	/// </returns>
	/// <remarks>
	/// This method provides access to all middleware types that have been registered
	/// through the middleware registration system, enabling dynamic middleware
	/// discovery and inspection.
	/// </remarks>
	Task<IImmutableList<Type>> QueryMiddlewares();
}
