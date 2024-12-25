using System.Collections.Immutable;

namespace Connected;
public static class Components
{
	private static readonly List<Type> _services;
	private static readonly List<Type> _serviceOperations;
	private static readonly List<Type> _middlewares;
	private static readonly List<Type> _caches;
	private static readonly List<Type> _dispatchers;
	private static readonly List<Type> _dispatcherJobs;
	private static readonly List<Type> _claimProviders;
	private static readonly List<Type> _dtos;

	static Components()
	{
		_services = [];
		_serviceOperations = [];
		_middlewares = [];
		_caches = [];
		_dispatchers = [];
		_dispatcherJobs = [];
		_claimProviders = [];
		_dtos = [];
	}

	public static ImmutableList<Type> Services => [.. _services];
	public static ImmutableList<Type> ServiceOperations => [.. _serviceOperations];
	public static ImmutableList<Type> Middlewares => [.. _middlewares];
	public static ImmutableList<Type> Caches => [.. _caches];
	public static ImmutableList<Type> Dispatchers => [.. _dispatchers];
	public static ImmutableList<Type> DispatcherJobs => [.. _dispatcherJobs];
	public static ImmutableList<Type> ClaimProviders => [.. _claimProviders];
	public static ImmutableList<Type> Dtos => [.. _dtos];

	public static void AddDispatcherJob(Type type)
	{
		_dispatcherJobs.Add(type);
	}

	public static void AddDispatcher(Type type)
	{
		_dispatchers.Add(type);
	}

	public static void AddService(Type type)
	{
		_services.Add(type);
	}

	public static void AddServiceOperation(Type type)
	{
		_serviceOperations.Add(type);
	}

	public static void AddMiddleware(Type type)
	{
		_middlewares.Add(type);
	}

	public static void AddCache(Type type)
	{
		_caches.Add(type);
	}

	public static void AddClaimProvider(Type type)
	{
		_claimProviders.Add(type);
	}

	public static void AddDto(Type type)
	{
		_dtos.Add(type);
	}
}