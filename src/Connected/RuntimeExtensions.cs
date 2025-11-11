using Connected.Annotations;
using Connected.Caching;
using Connected.Collections.Concurrent;
using Connected.Reflection;
using Connected.Services;
using Connected.Workers;
using Grpc.Core;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System.Reflection;

namespace Connected;
/// <summary>
/// Provides extension methods for registering and mapping runtime dependencies such as services,
/// middleware, caches, dispatchers, and gRPC endpoints.
/// </summary>
/// <remarks>
/// Maintains internal registries for discovered gRPC services and pending service registrations ordered by priority.
/// These collections are populated during dependency discovery and consumed during application startup to wire DI
/// and endpoint mapping.
/// </remarks>
public static class RuntimeExtensions
{
	/// <summary>
	/// Gets the list of discovered gRPC service types to be mapped at startup.
	/// </summary>
	internal static List<Type> GrpcServices { get; } = [];
	/// <summary>
	/// Gets the dictionary of pending service registrations keyed by service interface type, with each entry
	/// holding a list of candidate implementations ordered by priority.
	/// </summary>
	internal static Dictionary<Type, List<ServiceRegistrationDescriptor>> Services { get; } = [];
	/// <summary>
	/// Registers all discovered services into the <paramref name="services"/> collection by selecting the highest-priority
	/// implementation for each interface and applying the appropriate lifetime scope.
	/// </summary>
	/// <param name="services">The service collection to populate.</param>
	public static void RegisterServices(this IServiceCollection services)
	{
		/*
		 * Iterate pending service registrations. For each interface select the implementation with the highest priority
		 * then register using the configured scope (singleton, scoped, transient).
		 */
		foreach (var service in Services)
		{
			var target = service.Value.OrderByDescending(f => f.Priority).FirstOrDefault();

			if (target is null)
				continue;

			switch (target.Scope)
			{
				case ServiceRegistrationScope.Singleton:
					services.AddSingleton(service.Key, target.Type);
					break;
				case ServiceRegistrationScope.Scoped:
					services.AddScoped(service.Key, target.Type);
					break;
				case ServiceRegistrationScope.Transient:
					services.AddTransient(service.Key, target.Type);
					break;
			}
		}
		/*
		 * Clear the pending registrations after processing to prevent duplicate registration on subsequent calls.
		 */
		Services.Clear();
	}
	/// <summary>
	/// Discovers and registers all eligible types from the given <paramref name="assembly"/> into the service collection.
	/// </summary>
	/// <param name="services">The service collection to populate.</param>
	/// <param name="assembly">The assembly to scan for dependency types.</param>
	/// <returns>The service collection for chaining.</returns>
	public static IServiceCollection AddDependency(this IServiceCollection services, Assembly assembly)
	{
		/*
		 * Enumerate all concrete types in the assembly and dispatch to specialized registration methods for each
		 * category (service, cache, dispatcher, middleware, etc.). Trigger discovery event for external handlers.
		 */
		foreach (var type in assembly.GetTypes())
		{
			if (type.IsAbstract || !type.IsClass)
				continue;

			var typeRef = new List<Type>();

			AddService(type, services, false);
			AddServiceOperation(type, services, false);
			AddCache(type, services, false);
			AddDispatcher(type, services, false);
			AddDispatcherJob(type, services, false);
			AddAmbientValue(type, services, false, typeRef);
			AddMiddleware(type, services, false, typeRef);
			AddDto(type, services, false);
			AddHostedService(type, services, false);

			if (CanRegister(type, false))
				Application.TriggerDiscoverType(services, type);
		}

		return services;
	}
	/// <summary>
	/// Maps gRPC service endpoints for all eligible types in the given <paramref name="assembly"/>.
	/// </summary>
	/// <param name="app">The web application builder.</param>
	/// <param name="assembly">The assembly to scan for gRPC service types.</param>
	public static void MapDependency(this WebApplication app, Assembly assembly)
	{
		/*
		 * Iterate concrete types and invoke gRPC mapping logic for those decorated with BindServiceMethodAttribute.
		 */
		foreach (var type in assembly.GetTypes())
		{
			if (type.IsAbstract || !type.IsClass)
				continue;

			MapGrpcService(type, app, false);
		}
	}
	/// <summary>
	/// Maps a gRPC service endpoint if the type is eligible and decorated with <see cref="BindServiceMethodAttribute"/>.
	/// </summary>
	/// <param name="type">The type to inspect for gRPC service mapping.</param>
	/// <param name="app">The web application builder.</param>
	/// <param name="manual">True if this is a manual registration; false for automatic discovery.</param>
	private static void MapGrpcService(Type type, WebApplication app, bool manual)
	{
		/*
		 * Skip types that fail registration checks or lack a BindServiceMethodAttribute on their base type.
		 * Use reflection to invoke the generic RegisterGrpcService method for the concrete service type.
		 */
		if (!CanRegister(type, manual))
			return;

		if (type.BaseType?.FindAttribute<BindServiceMethodAttribute>() is null)
			return;

		var method = typeof(RuntimeExtensions).GetMethod(nameof(RegisterGrpcService), BindingFlags.Static | BindingFlags.NonPublic)?.MakeGenericMethod(type);

		method?.Invoke(null, [app]);

		GrpcServices.Add(type);
	}
	/// <summary>
	/// Registers a single service operation type into the service collection as transient.
	/// </summary>
	/// <param name="type">The service operation type.</param>
	/// <param name="services">The service collection.</param>
	public static void AddServiceOperation(Type type, IServiceCollection services)
	{
		AddServiceOperation(type, services, true);
	}
	/// <summary>
	/// Registers a service operation type if eligible.
	/// </summary>
	/// <param name="type">The service operation type.</param>
	/// <param name="services">The service collection.</param>
	/// <param name="manual">True if this is a manual registration; false for automatic discovery.</param>
	private static void AddServiceOperation(Type type, IServiceCollection services, bool manual)
	{
		/*
		 * Check registration eligibility and whether the type represents a service operation. Register as transient.
		 */
		if (CanRegister(type, manual) && type.IsServiceOperation())
			services.AddTransient(type);
	}
	/// <summary>
	/// Registers a single service type into the pending registration queue with appropriate scope and priority.
	/// </summary>
	/// <param name="type">The service implementation type.</param>
	/// <param name="services">The service collection.</param>
	public static void AddService(Type type, IServiceCollection services)
	{
		AddService(type, services, true);
	}
	/// <summary>
	/// Registers a service type by inspecting for <see cref="ServiceAttribute"/> on interfaces or the class itself.
	/// </summary>
	/// <param name="type">The service implementation type.</param>
	/// <param name="services">The service collection.</param>
	/// <param name="manual">True if this is a manual registration; false for automatic discovery.</param>
	private static void AddService(Type type, IServiceCollection services, bool manual)
	{
		/*
		 * Determine registration eligibility. Inspect interfaces for [Service] attribute and enqueue each service
		 * contract with the implementing type, scope, and priority. If the class itself is marked [Service], enqueue
		 * the class as its own service interface.
		 */
		if (!CanRegister(type, manual))
			return;

		var interfaces = type.GetInterfaces();
		var implementsService = false;

		foreach (var i in interfaces)
		{
			if (i.GetCustomAttribute<ServiceAttribute>() is ServiceAttribute att)
			{
				implementsService = true;
				var priority = type.FindAttribute<PriorityAttribute>()?.Priority ?? 0;

				switch (att.Scope)
				{
					case ServiceRegistrationScope.Singleton:
						EnqueueService(i, type, ServiceRegistrationScope.Singleton, priority);
						break;
					case ServiceRegistrationScope.Scoped:
						EnqueueService(i, type, ServiceRegistrationScope.Scoped, priority);
						break;
					case ServiceRegistrationScope.Transient:
						EnqueueService(i, type, ServiceRegistrationScope.Transient, priority);
						break;
					default:
						throw new NotSupportedException();
				}
			}
		}

		if (implementsService)
			return;

		if (type.GetCustomAttribute<ServiceAttribute>() is ServiceAttribute att2)
		{
			var priority = type.FindAttribute<PriorityAttribute>()?.Priority ?? 0;

			switch (att2.Scope)
			{
				case ServiceRegistrationScope.Singleton:
					EnqueueService(type, type, ServiceRegistrationScope.Singleton, priority);
					break;
				case ServiceRegistrationScope.Scoped:
					EnqueueService(type, type, ServiceRegistrationScope.Scoped, priority);
					break;
				case ServiceRegistrationScope.Transient:
					EnqueueService(type, type, ServiceRegistrationScope.Transient, priority);
					break;
				default:
					throw new NotSupportedException();
			}
		}
	}
	/// <summary>
	/// Adds a service registration descriptor to the pending registrations dictionary.
	/// </summary>
	/// <param name="interfaceType">The service interface type.</param>
	/// <param name="implementationType">The implementation type.</param>
	/// <param name="scope">The service registration scope.</param>
	/// <param name="priority">The priority for resolving multiple implementations.</param>
	private static void EnqueueService(Type interfaceType, Type implementationType, ServiceRegistrationScope scope, int priority)
	{
		/*
		 * Add the implementation descriptor to the list for the given interface. Create the list if it does not exist.
		 */
		if (Services.TryGetValue(interfaceType, out var existing))
			existing.Add(new ServiceRegistrationDescriptor(scope, priority, implementationType));
		else
			Services.Add(interfaceType, [new ServiceRegistrationDescriptor(scope, priority, implementationType)]);
	}
	/// <summary>
	/// Registers a hosted service (worker) if the type implements <see cref="IWorker"/>.
	/// </summary>
	/// <param name="type">The worker type.</param>
	/// <param name="services">The service collection.</param>
	/// <param name="manual">True if this is a manual registration; false for automatic discovery.</param>
	private static void AddHostedService(Type type, IServiceCollection services, bool manual)
	{
		/*
		 * Check registration eligibility and IWorker interface implementation. Use reflection to invoke generic
		 * registration method for hosted services.
		 */
		if (!CanRegister(type, manual))
			return;

		if (!type.ImplementsInterface<IWorker>())
			return;

		var method = typeof(RuntimeExtensions).GetMethod(nameof(RuntimeExtensions.RegisterHostedService), BindingFlags.Static | BindingFlags.NonPublic)?.MakeGenericMethod(type);

		method?.Invoke(null, [services]);
	}
	/// <summary>
	/// Generic helper method to register a hosted service of type <typeparamref name="TService"/>.
	/// </summary>
	/// <typeparam name="TService">The hosted service type.</typeparam>
	/// <param name="services">The service collection.</param>
	private static void RegisterHostedService<TService>(IServiceCollection services)
		where TService : class, IHostedService
	{
		/*
		 * Generic helper to invoke AddHostedService on the service collection for the concrete worker type.
		 */
		services.AddHostedService<TService>();
	}
	/// <summary>
	/// Generic helper method to map a gRPC service endpoint for type <typeparamref name="TService"/>.
	/// </summary>
	/// <typeparam name="TService">The gRPC service type.</typeparam>
	/// <param name="app">The web application builder.</param>
	private static void RegisterGrpcService<TService>(WebApplication app)
		where TService : class
	{
		/*
		 * Generic helper to map a gRPC service endpoint for the concrete service type.
		 */
		app.MapGrpcService<TService>();
	}
	/// <summary>
	/// Registers a middleware type into the service collection.
	/// </summary>
	/// <param name="type">The middleware type.</param>
	/// <param name="services">The service collection.</param>
	public static void AddMiddleware(Type type, IServiceCollection services)
	{
		AddMiddleware(type, services, true, null);
	}
	/// <summary>
	/// Registers a middleware type into the service collection if eligible, avoiding duplicate registrations via typeRef.
	/// </summary>
	/// <param name="type">The middleware type.</param>
	/// <param name="services">The service collection.</param>
	/// <param name="manual">Whether registration is manual.</param>
	/// <param name="typeRef">Optional list tracking already-registered types to avoid duplicates.</param>
	public static void AddMiddleware(Type type, IServiceCollection services, bool manual, List<Type>? typeRef)
	{
		/*
		 * Check registration eligibility and IMiddleware interface presence. Register unless already tracked in typeRef.
		 */
		var fullName = typeof(IMiddleware).FullName;

		if (fullName is null)
			return;

		if (!CanRegister(type, manual) || type.GetInterface(fullName) is null)
			return;

		if (typeRef is null || !typeRef.Contains(type))
			services.AddMiddleware(type);
	}
	/// <summary>
	/// Registers a cache container type into the service collection.
	/// </summary>
	/// <param name="type">The cache container type.</param>
	/// <param name="services">The service collection.</param>
	public static void AddCache(Type type, IServiceCollection services)
	{
		AddCache(type, services, true);
	}
	/// <summary>
	/// Registers a cache container type if it implements <see cref="ICacheContainer{TEntry, TKey}"/>.
	/// </summary>
	/// <param name="type">The cache container type.</param>
	/// <param name="services">The service collection.</param>
	/// <param name="manual">True if this is a manual registration; false for automatic discovery.</param>
	private static void AddCache(Type type, IServiceCollection services, bool manual)
	{
		/*
		 * Check registration eligibility and ICacheContainer interface presence. Register all cache-related interfaces
		 * implemented by the type with scoped lifetime.
		 */
		if (!CanRegister(type, manual) || typeof(ICacheContainer<,>).FullName is not string fullName)
			return;

		if (type.GetInterface(fullName) is null)
			return;

		foreach (var itf in type.GetInterfaces())
		{
			if (itf.GetInterface(fullName) is not null)
				services.Add(ServiceDescriptor.Scoped(itf, type));
		}
	}
	/// <summary>
	/// Registers a dispatcher type into the service collection.
	/// </summary>
	/// <param name="type">The dispatcher type.</param>
	/// <param name="services">The service collection.</param>
	public static void AddDispatcher(Type type, IServiceCollection services)
	{
		AddDispatcher(type, services, true);
	}
	/// <summary>
	/// Registers a dispatcher type if it implements <see cref="IDispatcher{TArgs, TJob}"/>.
	/// </summary>
	/// <param name="type">The dispatcher type.</param>
	/// <param name="services">The service collection.</param>
	/// <param name="manual">True if this is a manual registration; false for automatic discovery.</param>
	private static void AddDispatcher(Type type, IServiceCollection services, bool manual)
	{
		/*
		 * Check registration eligibility and IDispatcher interface presence. Register with transient lifetime.
		 */
		if (!CanRegister(type, manual) || typeof(IDispatcher<,>).FullName is not string fullName)
			return;

		if (type.GetInterface(fullName) is null)
			return;

		services.AddTransient(type);
	}
	/// <summary>
	/// Registers a dispatcher job type into the service collection.
	/// </summary>
	/// <param name="type">The dispatcher job type.</param>
	/// <param name="services">The service collection.</param>
	public static void AddDispatcherJob(Type type, IServiceCollection services)
	{
		AddDispatcherJob(type, services, true);
	}
	/// <summary>
	/// Registers a dispatcher job type if it implements <see cref="IDispatcherJob{TDto}"/>.
	/// </summary>
	/// <param name="type">The dispatcher job type.</param>
	/// <param name="services">The service collection.</param>
	/// <param name="manual">True if this is a manual registration; false for automatic discovery.</param>
	private static void AddDispatcherJob(Type type, IServiceCollection services, bool manual)
	{
		/*
		 * Check registration eligibility and IDispatcherJob interface presence. Register with transient lifetime.
		 */
		if (!CanRegister(type, manual) || typeof(IDispatcherJob<>).FullName is not string fullName)
			return;

		if (type.GetInterface(fullName) is null)
			return;

		services.AddTransient(type);
	}
	/// <summary>
	/// Registers ambient value provider types implementing <see cref="IAmbientProvider{T}"/>.
	/// </summary>
	/// <param name="type">The ambient provider type.</param>
	/// <param name="services">The service collection.</param>
	/// <param name="manual">True if this is a manual registration; false for automatic discovery.</param>
	/// <param name="typeRef">Optional list tracking already-registered types to avoid duplicates.</param>
	private static void AddAmbientValue(Type type, IServiceCollection services, bool manual, List<Type>? typeRef)
	{
		/*
		 * Check registration eligibility and IAmbientProvider interface presence. For each implemented ambient interface,
		 * register with scoped lifetime and add to middleware registry to enable context injection. Track in typeRef.
		 */
		if (!CanRegister(type, manual))
			return;

		var fullName = typeof(IAmbientProvider<>).FullName;

		if (fullName is null)
			return;

		if (type.GetInterface(fullName) is null)
			return;

		var ambientType = typeof(IAmbientProvider<>);
		var interfaces = type.GetInterfaces();

		foreach (var i in interfaces)
		{
			if (i == typeof(IAmbientProvider<>))
				continue;

			if (i.GetInterfaces().Any(f => f.IsGenericType && f.GetGenericTypeDefinition() == ambientType))
			{
				services.Add(ServiceDescriptor.Scoped(i, type));

				CoreExtensions.AddMiddleware(type);

				typeRef?.Add(type);
			}
		}
	}
	/// <summary>
	/// Registers a DTO type into the service collection for all directly implemented DTO interfaces.
	/// </summary>
	/// <param name="type">The DTO type.</param>
	/// <param name="services">The service collection.</param>
	public static void AddDto(Type type, IServiceCollection services)
	{
		AddDto(type, services, true);
	}
	/// <summary>
	/// Registers DTO interfaces implemented by the type, excluding the base <see cref="IDto"/> interface.
	/// </summary>
	/// <param name="type">The DTO type.</param>
	/// <param name="services">The service collection.</param>
	/// <param name="manual">True if this is a manual registration; false for automatic discovery.</param>
	private static void AddDto(Type type, IServiceCollection services, bool manual)
	{
		/*
		 * Check registration eligibility and IDto interface presence. Register each directly implemented DTO interface
		 * (excluding IDto itself) with transient lifetime. Handle generic types by registering their definitions.
		 */
		var fullName = typeof(IDto).FullName;

		if (fullName is null)
			return;

		if (!CanRegister(type, manual) || type.GetInterface(fullName) is null)
			return;

		var allInterfaces = type.GetInterfaces();
		var implementedInterfaces = allInterfaces.Except(allInterfaces.SelectMany(f => f.GetInterfaces()));

		foreach (var i in implementedInterfaces)
		{
			if (i == typeof(IDto))
				continue;

			if (i.IsAssignableTo(typeof(IDto)))
			{
				var interfaceDefinition = i.IsGenericType ? i.GetGenericTypeDefinition() : i;
				var typeDefinition = type.IsGenericType ? type.GetGenericTypeDefinition() : type;

				services.AddTransient(interfaceDefinition, typeDefinition);
			}
		}
	}
	/// <summary>
	/// Determines whether a type is eligible for automatic registration based on its attributes and the registration mode.
	/// </summary>
	/// <param name="type">The type to check.</param>
	/// <param name="manual">True if this is a manual registration; false for automatic discovery.</param>
	/// <returns>True if the type can be registered; otherwise false.</returns>
	private static bool CanRegister(Type type, bool manual)
	{
		/*
		 * Return false if the type is abstract. For manual registration return true unconditionally. For automatic
		 * registration check the ServiceRegistrationAttribute mode or default to auto if attribute is absent.
		 */
		if (type.IsAbstract)
			return false;

		if (manual)
			return true;

		return type.GetCustomAttribute<ServiceRegistrationAttribute>() is not ServiceRegistrationAttribute att || att.Mode == ServiceRegistrationMode.Auto;
	}
}
