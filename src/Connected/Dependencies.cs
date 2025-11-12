using Connected.Collections;
using Connected.Reflection;
using Connected.Runtime;
using System.Collections.Immutable;
using System.Reflection;

namespace Connected;
/// <summary>
/// Maintains a registry of dependency assemblies and their discovered startup components.
/// </summary>
/// <remarks>
/// Tracks a unique list of loaded <see cref="Assembly"/> instances and creates/keeps instances of
/// <see cref="Runtime.IStartup"/> discovered via reflection. Startup instances are kept ordered by priority
/// to ensure a deterministic execution sequence during application bootstrapping.
/// </remarks>
internal static class Dependencies
{
	private static readonly List<Assembly> _all;
	private static readonly List<Runtime.IStartup> _startups;
	/// <summary>
	/// Registers an assembly for dependency discovery, instantiating and ordering any <see cref="IStartup"/>
	/// implementations found within it.
	/// </summary>
	/// <param name="assembly">The assembly to register.</param>
	/// <remarks>
	/// Re-registration of the same assembly is ignored. For each non-abstract type implementing
	/// <see cref="IStartup"/>, an instance is created and added to the internal startup list, which is then
	/// sorted by priority.
	/// </remarks>
	/// <exception cref="ReflectionTypeLoadException">Thrown if type discovery fails for the provided assembly.</exception>
	/// <exception cref="NullReferenceException">Thrown when a startup instance cannot be created for a discovered type.</exception>
	public static void Register(Assembly assembly)
	{
		/*
		 * Guard against duplicate registration to avoid redundant startup instances and repeated endpoint mappings.
		 */
		if (_all.Contains(assembly))
			return;
		/*
		 * Iterate all loadable types, ignoring abstract definitions. For each concrete type implementing IStartup,
		 * create an instance, append it to the startup list, and sort by priority so execution order is stable.
		 */
		foreach (var type in assembly.GetTypes())
		{
			if (type.IsAbstract)
				continue;

			if (type.ImplementsInterface<IStartup>())
			{
				var instance = Activator.CreateInstance(type) as IStartup
					?? throw new NullReferenceException($"{SR.ErrCreateStartupInstance} ('{type.FullName}, {type.Assembly.GetName().Name}')");
				_startups.Add(instance);
				_startups.SortByPriority();
			}
		}
		/*
		 * Track the assembly as processed to prevent re-processing on subsequent calls.
		 */
		_all.Add(assembly);
	}
	static Dependencies()
	{
		_all = [];
		_startups = [];
	}
	/// <summary>
	/// Gets the list of all registered assemblies.
	/// </summary>
	public static IImmutableList<Assembly> All => [.. _all];
	/// <summary>
	/// Gets the ordered list of discovered startup instances.
	/// </summary>
	public static IImmutableList<Runtime.IStartup> Startups => [.. _startups];
}