using Connected.Reflection;
using Connected.Runtime;
using System.Collections.Immutable;
using System.Reflection;

namespace Connected;

internal static class MicroServices
{
	private static readonly List<Assembly> _all;
	private static readonly List<Runtime.IStartup> _startups;

	public static void Register(Assembly assembly)
	{
		if (_all.Contains(assembly))
			return;

		foreach (var type in assembly.GetTypes())
		{
			if (type.IsAbstract)
				continue;

			if (type.ImplementsInterface<IStartup>())
			{
				var instance = Activator.CreateInstance(type) as IStartup ?? throw new NullReferenceException($"{SR.ErrCreateStartupInstance} ('{type.FullName}, {type.Assembly.GetName().Name}')");

				_startups.Add(instance);
				_startups.SortByPriority();
			}
		}

		_all.Add(assembly);
	}

	static MicroServices()
	{
		_all = [];
		_startups = [];
	}

	public static IImmutableList<Assembly> All => [.. _all];
	public static IImmutableList<Runtime.IStartup> Startups => [.. _startups];
}