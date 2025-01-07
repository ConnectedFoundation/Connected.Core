using Connected.Annotations;
using Connected.Caching;
using Connected.Collections.Concurrent;
using Connected.Membership.Claims;
using Connected.Services;
using Microsoft.Extensions.DependencyInjection;
using System.Reflection;

namespace Connected;

public static class RuntimeExtensions
{
	public static IServiceCollection AddMicroService(this IServiceCollection services, Assembly assembly)
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
			AddClaimProvider(type, services, false);
			AddMiddleware(type, services, false, typeRef);
			AddDto(type, services, false);
		}

		return services;
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

		foreach (var i in interfaces)
		{
			if (i.GetCustomAttribute<ServiceAttribute>() is ServiceAttribute att)
			{
				switch (att.Scope)
				{
					case ServiceRegistrationScope.Singleton:
						services.Add(ServiceDescriptor.Singleton(i, type));
						break;
					case ServiceRegistrationScope.Scoped:
						services.Add(ServiceDescriptor.Scoped(i, type));
						break;
					case ServiceRegistrationScope.Transient:
						services.Add(ServiceDescriptor.Transient(i, type));
						break;
					default:
						throw new NotSupportedException();
				}
			}
		}
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

				typeRef?.Add(type);
			}
		}
	}

	public static void AddClaimProvider(Type type, IServiceCollection services)
	{
		AddClaimProvider(type, services, true);
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

	private static void AddClaimProvider(Type type, IServiceCollection services, bool manual)
	{
		var fullName = typeof(IClaimProvider).FullName;

		if (fullName is null)
			return;

		if (!CanRegister(type, manual) || type.GetInterface(fullName) is null)
			return;

		services.Add(ServiceDescriptor.Scoped(type, type));
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
