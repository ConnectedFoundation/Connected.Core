﻿using System.Reflection;

namespace Connected;

public static class MicroServices
{
	private static readonly List<Assembly> _all;
	private static readonly List<Runtime.IStartup> _startups;

	public static void Register(string assemblyName)
	{
		var fileName = ResolveFileName(assemblyName);
		var name = AssemblyName.GetAssemblyName(fileName);
		var assembly = Assembly.Load(name);

		_all.Add(assembly);

		foreach (var type in assembly.GetTypes())
		{
			if (IsStartup(type))
			{
				var instance = Activator.CreateInstance(type) ?? throw new NullReferenceException($"{SR.ErrCreateStartupInstance} ('{type.Name}, {assembly.GetName().Name}')");

				if (instance is not Runtime.IStartup startup)
					throw new NullReferenceException($"{SR.ErrCreateStartupInstance} ('{type.Name}, {assembly.GetName().Name}')");

				_startups.Add(startup);
			}
		}

		_startups.SortByPriority();
	}

	private static string ResolveFileName(string assemblyName)
	{
		if (!assemblyName.EndsWith(".dll"))
			assemblyName = $"{assemblyName}.dll";

		if (Path.IsPathFullyQualified(assemblyName))
			return assemblyName;

		return Path.Combine(AppDomain.CurrentDomain.BaseDirectory, assemblyName);
	}

	static MicroServices()
	{
		_all = [];
		_startups = [];
	}

	public static ImmutableList<Assembly> All => [.. _all];
	public static ImmutableList<Runtime.IStartup> Startups => [.. _startups];

	public static Dictionary<Type, Type> QueryInterfaces<TAttribute>(this Assembly assembly) where TAttribute : Attribute
	{
		var result = new Dictionary<Type, Type>();

		foreach (var type in assembly.GetTypes())
		{
			if (type.IsAbstract || !type.IsClass)
				continue;

			var interfaces = type.GetInterfaces();

			foreach (var i in interfaces)
			{
				if (i.GetCustomAttribute<TAttribute>() is not null)
					result.Add(i, type);
			}
		}

		return result;
	}

	public static Dictionary<Type, Type> QueryImplementations<T>(this Assembly assembly)
	{
		var result = new Dictionary<Type, Type>();

		foreach (var type in assembly.GetTypes())
		{
			if (type.IsAbstract || !type.IsClass)
				continue;

			var fullName = typeof(T).FullName;

			if (fullName is null)
				continue;

			if (type.GetInterface(fullName) is null)
				continue;

			var interfaces = type.GetInterfaces();

			foreach (var i in interfaces)
			{
				if (i.GetInterface(fullName) is not null)
					result.Add(i, type);
			}
		}

		return result;
	}

	public static ImmutableList<Type> QueryImplementations<T>()
	{
		var target = typeof(T).FullName;

		if (target is null)
			return ImmutableList<Type>.Empty;

		var result = new List<Type>();

		foreach (var microService in _all)
		{
			var types = microService.GetTypes();

			foreach (var type in types)
			{
				if (type.IsAbstract || type.IsPrimitive || type.IsInterface)
					continue;

				if (type.GetInterface(target) is not null)
					result.Add(type);
			}
		}

		result.SortByPriority();

		return result.ToImmutableList();
	}

	private static bool IsStartup(Type type)
	{
		if (type.IsAbstract)
			return false;

		var interfaces = type.GetInterfaces();

		foreach (var i in interfaces)
		{
			if (i == typeof(Runtime.IStartup))
				return true;
		}

		return false;
	}
}