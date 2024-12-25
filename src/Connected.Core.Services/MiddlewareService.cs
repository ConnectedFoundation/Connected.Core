using Connected.Annotations;
using Connected.Runtime;
using Connected.Services.Middleware;
using System.Collections.Concurrent;
using System.Collections.Immutable;
using System.Reflection;

namespace Connected.Services;

internal class MiddlewareService : IMiddlewareService
{
	private static readonly Lock _lock = new();

	static MiddlewareService()
	{
		Endpoints = new();
	}

	public MiddlewareService(IServiceProvider services, IRuntimeService runtime)
	{
		Services = services;
		Runtime = runtime;

		if (!IsInitialized)
		{
			lock (_lock)
			{
				if (!IsInitialized)
					Initialize();
			}
		}
	}

	private static bool IsInitialized { get; set; }
	private static ConcurrentDictionary<string, List<Type>> Endpoints { get; set; }
	private IServiceProvider Services { get; }
	private IRuntimeService Runtime { get; }

	public async Task<ImmutableList<TEndpoint>> Query<TEndpoint>() where TEndpoint : IMiddleware
	{
		return await Query<TEndpoint>(null);
	}

	public async Task<ImmutableList<IMiddleware>> Query(Type type)
	{
		return await Query(type, null);
	}

	public async Task<ImmutableList<TEndpoint>> Query<TEndpoint>(ICallerContext? context) where TEndpoint : IMiddleware
	{
		var key = typeof(TEndpoint).FullName;

		if (key is null || Endpoints is null)
			return [];

		if (!Endpoints.TryGetValue(key, out List<Type>? items) || items is null)
			return [];

		var result = new List<TEndpoint>();

		foreach (var type in items)
		{
			if (!Validate(context, type))
				continue;

			if (Services.GetService(type) is object service)
				result.Add((TEndpoint)service);
		}

		result.SortByPriority();

		var tasks = new List<Task>();

		foreach (var r in result)
			tasks.Add(r.Initialize());

		Task.WaitAll([.. tasks]);

		return await Task.FromResult(result.ToImmutableList());
	}

	public async Task<ImmutableList<IMiddleware>> Query(Type type, ICallerContext? context)
	{
		var key = type.FullName;

		if (key is null || Endpoints is null)
			return [];

		if (!Endpoints.TryGetValue(key, out List<Type>? items) || items is null)
			return [];

		var result = new List<IMiddleware>();

		foreach (var t in items)
		{
			if (!Validate(context, t))
				continue;

			if (Services.GetService(t) is IMiddleware service)
				result.Add(service);
		}

		result.SortByPriority();

		foreach (var r in result)
			await r.Initialize();

		return [.. result];
	}
	public async Task<TEndpoint?> First<TEndpoint>()
		where TEndpoint : IMiddleware
	{
		var key = typeof(TEndpoint).FullName;

		if (key is null || Endpoints is null)
			return default;

		if (!Endpoints.TryGetValue(key, out List<Type>? items) || items is null)
			return default;

		var types = new List<Type>();

		foreach (var type in items)
			types.Add(type);

		if (types.Count == 0)
			return default;

		types.SortByPriority();

		foreach (var type in types)
		{
			if (!Validate(null, type))
				continue;

			if (Services.GetService(type) is object service)
			{
				var r = (TEndpoint)service;
				await r.Initialize();

				return r;
			}
		}

		return default;
	}

	public async Task<IMiddleware?> First(Type type)
	{
		var key = type.FullName;

		if (key is null || Endpoints is null)
			return default;

		if (!Endpoints.TryGetValue(key, out List<Type>? items) || items is null)
			return default;

		var types = new List<Type>();

		foreach (var t in items)
			types.Add(t);

		if (types.Count == 0)
			return default;

		types.SortByPriority();

		foreach (var t in types)
		{
			if (!Validate(null, t))
				continue;

			if (Services.GetService(t) is IMiddleware service)
			{
				await service.Initialize();

				return service;
			}
		}

		return default;
	}

	private static bool Validate(ICallerContext? context, Type type)
	{
		if (context is null)
			return true;

		var attributes = type.GetCustomAttributes();
		var targetType = typeof(MiddlewareAttribute<>).FullName;
		var hasMiddlewareAttribute = false;

		foreach (var attribute in attributes)
		{
			if (!attribute.GetType().IsGenericType)
				continue;

			var attributeType = attribute.GetType().GetGenericTypeDefinition().FullName;

			if (!string.Equals(attributeType, targetType, StringComparison.Ordinal))
				continue;

			hasMiddlewareAttribute = true;

			var method = attribute.GetType().GetProperty(nameof(MiddlewareAttribute<object>.Method));

			if (method is null)
				continue;

			if (!string.Equals(Convert.ToString(method.GetValue(attribute)), context.Method, StringComparison.Ordinal))
				continue;

			var argument = attribute.GetType().GetGenericArguments()[0];

			if (argument == context.Sender?.GetType())
				return true;

			var argumentName = argument.FullName;

			if (argumentName is not null && argument.IsInterface)
			{
				if (context.Sender is Type senderType && senderType.GetInterface(argumentName) is not null)
					return true;
				else if (context.Sender?.GetType().GetInterface(argumentName) is not null)
					return true;
			}
		}

		return !hasMiddlewareAttribute;
	}

	private void Initialize()
	{
		IsInitialized = true;

		foreach (var middleware in Runtime.QueryMiddlewares().Result)
		{
			var interfaces = middleware.GetImplementedMiddlewares();

			foreach (var i in interfaces)
			{
				var fullName = i.FullName;

				if (fullName is null)
					continue;
				/*
				 * There are two things to consider:
				 * - middleware can implement interface which inherits from middleware
				 * - middleware can implement middleware interface directly
				 * In the first case we must map all interfaces since they are probably
				 * registered in the DI and their corresponding types are not directly
				 * accessible via DI.
				 */
				foreach (var j in interfaces)
				{
					if (i == j)
						continue;

					if (j.IsAssignableTo(i))
					{
						if (Endpoints.TryGetValue(fullName, out List<Type>? interfaceList))
							interfaceList.Add(j);
						else
							Endpoints.TryAdd(fullName, [j]);
					}
				}
				/*
				 * We might add a check here in the future and add the type itself
				 * only if it not implements any interfaces.
				 */
				if (Endpoints.TryGetValue(fullName, out List<Type>? list))
					list.Add(middleware);
				else
					Endpoints.TryAdd(fullName, [middleware]);
			}
			/*
			 * Type directly implements middleware, no intermediate interfaces,
			 * let's map the type itself.
			 */
			if (interfaces.Count == 0)
			{
				if (string.IsNullOrWhiteSpace(middleware.FullName))
					continue;

				if (Endpoints.TryGetValue(middleware.FullName, out List<Type>? list))
					list.Add(middleware);
				else
					Endpoints.TryAdd(middleware.FullName, [middleware]);
			}
		}
	}
}
