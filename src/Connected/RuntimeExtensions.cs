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

public static class RuntimeExtensions
{
	internal static List<Type> GrpcServices { get; } = [];
	internal static Dictionary<Type, List<ServiceRegistrationDescriptor>> Services { get; } = [];
	public static void RegisterServices(this IServiceCollection services)
	{
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

		Services.Clear();
	}
	public static IServiceCollection AddDependency(this IServiceCollection services, Assembly assembly)
	{
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

	public static void MapDependency(this WebApplication app, Assembly assembly)
	{
		foreach (var type in assembly.GetTypes())
		{
			if (type.IsAbstract || !type.IsClass)
				continue;

			MapGrpcService(type, app, false);
		}
	}

	private static void MapGrpcService(Type type, WebApplication app, bool manual)
	{
		if (!CanRegister(type, manual))
			return;

		if (type.BaseType?.FindAttribute<BindServiceMethodAttribute>() is null)
			return;

		var method = typeof(RuntimeExtensions).GetMethod(nameof(RegisterGrpcService), BindingFlags.Static | BindingFlags.NonPublic)?.MakeGenericMethod(type);

		method?.Invoke(null, [app]);

		GrpcServices.Add(type);
	}

	public static void AddServiceOperation(Type type, IServiceCollection services)
	{
		AddServiceOperation(type, services, true);
	}

	private static void AddServiceOperation(Type type, IServiceCollection services, bool manual)
	{
		if (CanRegister(type, manual) && type.IsServiceOperation())
			services.AddTransient(type);
	}

	public static void AddService(Type type, IServiceCollection services)
	{
		AddService(type, services, true);
	}

	private static void AddService(Type type, IServiceCollection services, bool manual)
	{
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

	private static void EnqueueService(Type interfaceType, Type implementationType, ServiceRegistrationScope scope, int priority)
	{
		if (Services.TryGetValue(interfaceType, out var existing))
			existing.Add(new ServiceRegistrationDescriptor(scope, priority, implementationType));
		else
			Services.Add(interfaceType, [new ServiceRegistrationDescriptor(scope, priority, implementationType)]);
	}

	private static void AddHostedService(Type type, IServiceCollection services, bool manual)
	{
		if (!CanRegister(type, manual))
			return;

		if (!type.ImplementsInterface<IWorker>())
			return;

		var method = typeof(RuntimeExtensions).GetMethod(nameof(RuntimeExtensions.RegisterHostedService), BindingFlags.Static | BindingFlags.NonPublic)?.MakeGenericMethod(type);

		method?.Invoke(null, [services]);
	}

	private static void RegisterHostedService<TService>(IServiceCollection services)
		where TService : class, IHostedService
	{
		services.AddHostedService<TService>();
	}

	private static void RegisterGrpcService<TService>(WebApplication app)
		where TService : class
	{
		app.MapGrpcService<TService>();
	}

	public static void AddMiddleware(Type type, IServiceCollection services)
	{
		AddMiddleware(type, services, true, null);
	}

	public static void AddMiddleware(Type type, IServiceCollection services, bool manual, List<Type>? typeRef)
	{
		var fullName = typeof(IMiddleware).FullName;

		if (fullName is null)
			return;

		if (!CanRegister(type, manual) || type.GetInterface(fullName) is null)
			return;

		if (typeRef is null || !typeRef.Contains(type))
			services.AddMiddleware(type);
	}

	public static void AddCache(Type type, IServiceCollection services)
	{
		AddCache(type, services, true);
	}

	private static void AddCache(Type type, IServiceCollection services, bool manual)
	{
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

	public static void AddDispatcher(Type type, IServiceCollection services)
	{
		AddDispatcher(type, services, true);
	}

	private static void AddDispatcher(Type type, IServiceCollection services, bool manual)
	{
		if (!CanRegister(type, manual) || typeof(IDispatcher<,>).FullName is not string fullName)
			return;

		if (type.GetInterface(fullName) is null)
			return;

		services.AddTransient(type);
	}

	public static void AddDispatcherJob(Type type, IServiceCollection services)
	{
		AddDispatcherJob(type, services, true);
	}

	private static void AddDispatcherJob(Type type, IServiceCollection services, bool manual)
	{
		if (!CanRegister(type, manual) || typeof(IDispatcherJob<>).FullName is not string fullName)
			return;

		if (type.GetInterface(fullName) is null)
			return;

		services.AddTransient(type);
	}

	private static void AddAmbientValue(Type type, IServiceCollection services, bool manual, List<Type>? typeRef)
	{
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

	public static void AddDto(Type type, IServiceCollection services)
	{
		AddDto(type, services, true);
	}

	private static void AddDto(Type type, IServiceCollection services, bool manual)
	{
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

	private static bool CanRegister(Type type, bool manual)
	{
		if (type.IsAbstract)
			return false;

		if (manual)
			return true;

		return type.GetCustomAttribute<ServiceRegistrationAttribute>() is not ServiceRegistrationAttribute att || att.Mode == ServiceRegistrationMode.Auto;
	}
}
