using Connected.Annotations;
using Connected.Runtime;
using Connected.Services;
using System.Reflection;

namespace Connected.Net.Rest.OpenApi.Reflection;

/// <summary>
/// Discovers Http-accessible service operations by scanning the same dependency assemblies
/// and <see cref="ServiceAttribute"/> / <see cref="ServiceOperationAttribute"/> annotations
/// that Connected.Core's own REST resolution pipeline uses to map routes.
/// </summary>
internal static class ApiSurfaceScanner
{
	public static async Task<List<ApiOperationDescriptor>> Scan(IRuntimeService runtimeService)
	{
		var result = new List<ApiOperationDescriptor>();
		var visitedServices = new HashSet<Type>();
		var assemblies = await runtimeService.QueryDependencies();

		foreach (var assembly in assemblies)
		{
			foreach (var type in assembly.GetTypes())
			{
				/*
				 * Interfaces are abstract too, so this also filters out the service contracts
				 * themselves. Only concrete implementations tell us which services are actually
				 * registered and therefore reachable, exactly like ResolutionService does.
				 */
				if (type.IsAbstract || !type.IsClass)
					continue;

				var services = type.GetImplementedServices();

				if (services.Count > 0)
				{
					foreach (var service in services)
					{
						if (visitedServices.Add(service))
							CollectOperations(service, result);
					}
				}
				else if (type.GetCustomAttribute<ServiceAttribute>() is not null && visitedServices.Add(type))
					CollectOperations(type, result);
			}
		}

		return result;
	}

	private static void CollectOperations(Type serviceType, List<ApiOperationDescriptor> result)
	{
		var serviceUrl = ServiceUrlResolver.ResolveServiceUrl(serviceType);

		foreach (var method in serviceType.GetMethods(BindingFlags.Public | BindingFlags.Instance))
		{
			if (method.GetCustomAttribute<ServiceOperationAttribute>() is not ServiceOperationAttribute attribute || attribute.Verbs == ServiceOperationVerbs.None)
				continue;

			var url = $"{serviceUrl}/{ServiceUrlResolver.ResolveMethodUrl(method)}";

			if (!url.StartsWith('/'))
				url = $"/{url}";

			result.Add(new ApiOperationDescriptor(url, attribute.Verbs, serviceType, method));
		}
	}
}
