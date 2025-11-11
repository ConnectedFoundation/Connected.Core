namespace Connected.Runtime;

/// <summary>
/// Defines the lifecycle hooks for application startup and configuration.
/// </summary>
/// <remarks>
/// This interface provides a structured approach to application initialization by
/// exposing multiple configuration phases. Implementations can register services,
/// configure middleware pipelines, define endpoint routes, and perform custom
/// initialization logic. The startup lifecycle follows a specific sequence: Prepare,
/// ConfigureServices, Configure, ConfigureEndpoints, Initialize, and finally Start.
/// This phased approach ensures that components are configured in the correct order
/// and dependencies are available when needed. The IsUpdated property enables
/// hot-reload scenarios by indicating when a startup component has been modified.
/// </remarks>
public interface IStartup
{
	/// <summary>
	/// Gets a value indicating whether this startup component has been updated.
	/// </summary>
	/// <value>
	/// <c>true</c> if the startup component has been modified and requires reinitialization;
	/// otherwise, <c>false</c>.
	/// </value>
	/// <remarks>
	/// This property is used in hot-reload and dynamic update scenarios to determine
	/// whether the startup component needs to be reconfigured or reinitialized.
	/// </remarks>
	bool IsUpdated { get; }

	/// <summary>
	/// Prepares the startup component using the provided configuration.
	/// </summary>
	/// <param name="configuration">The application configuration instance.</param>
	/// <remarks>
	/// This is the first method called in the startup lifecycle, before service
	/// registration. Implementations should use this phase to read configuration
	/// settings and prepare any necessary state for subsequent configuration phases.
	/// This method is called synchronously during the early bootstrap phase.
	/// </remarks>
	void Prepare(IConfiguration configuration);

	/// <summary>
	/// Configures services in the dependency injection container.
	/// </summary>
	/// <param name="services">The service collection to configure.</param>
	/// <remarks>
	/// This method is called after Prepare and is used to register services, configure
	/// options, and set up dependency injection bindings. This is the primary phase
	/// for adding services to the application's service container.
	/// </remarks>
	void ConfigureServices(IServiceCollection services);

	/// <summary>
	/// Configures the application middleware pipeline.
	/// </summary>
	/// <param name="app">The application builder used to configure the request pipeline.</param>
	/// <param name="env">The web host environment information.</param>
	/// <remarks>
	/// This method is called after ConfigureServices and is used to add middleware
	/// components to the request processing pipeline. The order of middleware
	/// registration in this method determines the order of execution for incoming
	/// requests. The environment parameter allows for environment-specific
	/// middleware configuration.
	/// </remarks>
	void Configure(IApplicationBuilder app, IWebHostEnvironment env);

	/// <summary>
	/// Asynchronously configures endpoint routing for the application.
	/// </summary>
	/// <param name="builder">The endpoint route builder used to define routes.</param>
	/// <returns>A task that represents the asynchronous configuration operation.</returns>
	/// <remarks>
	/// This method is called after Configure and provides an opportunity to register
	/// API endpoints, MVC routes, SignalR hubs, and other routable components.
	/// This is the appropriate phase for defining the application's URL routing structure.
	/// </remarks>
	Task ConfigureEndpoints(IEndpointRouteBuilder builder);

	/// <summary>
	/// Asynchronously performs initialization logic after the host is built.
	/// </summary>
	/// <param name="host">The configured host instance.</param>
	/// <returns>A task that represents the asynchronous initialization operation.</returns>
	/// <remarks>
	/// This method is called after all configuration phases are complete but before
	/// the application starts processing requests. Implementations should use this
	/// phase to perform initialization tasks that require access to registered
	/// services, such as database seeding, cache warming, or validation checks.
	/// </remarks>
	Task Initialize(IHost host);

	/// <summary>
	/// Asynchronously starts the startup component.
	/// </summary>
	/// <returns>A task that represents the asynchronous start operation.</returns>
	/// <remarks>
	/// This is the final method called in the startup lifecycle, invoked after
	/// Initialize and when the application is ready to begin processing requests.
	/// Implementations can use this phase to start background workers, establish
	/// persistent connections, or perform other startup tasks that should occur
	/// when the application enters its running state.
	/// </remarks>
	Task Start();
}
