using System.Collections.Immutable;
using System.Reflection;

namespace Connected;

internal static class MicroServices
{
	private static readonly List<Assembly> _all;
	private static readonly List<Runtime.IStartup> _startups;

	public static void Register<TStartup>()
		where TStartup : Runtime.IStartup
	{
		var instance = (Runtime.IStartup)Activator.CreateInstance<TStartup>() ?? throw new NullReferenceException($"{SR.ErrCreateStartupInstance} ('{typeof(TStartup).Name}, {typeof(TStartup).Assembly.GetName().Name}')");

		_startups.Add(instance);
		_startups.SortByPriority();

		if (!_all.Contains(instance.GetType().Assembly))
			_all.Add(instance.GetType().Assembly);
	}

	static MicroServices()
	{
		_all = [];
		_startups = [];
	}

	public static ImmutableList<Assembly> All => [.. _all];
	public static ImmutableList<Runtime.IStartup> Startups => [.. _startups];
}