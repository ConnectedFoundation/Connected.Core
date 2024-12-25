using System.Globalization;
using System.Reflection;
using System.Runtime.Loader;

namespace Connected.Reflection;

public static class Types
{
	private static Dictionary<Type, string> AliasedTypes = new Dictionary<Type, string>
	{
		{typeof(int), "int"},
		{typeof(uint), "uint"},
		{typeof(long), "long"},
		{typeof(ulong), "ulong"},
		{typeof(short), "short"},
		{typeof(ushort), "ushort"},
		{typeof(byte), "byte"},
		{typeof(sbyte), "sbyte"},
		{typeof(bool), "bool"},
		{typeof(float), "float"},
		{typeof(double), "double"},
		{typeof(decimal), "decimal"},
		{typeof(char), "char"},
		{typeof(string), "string"},
		{typeof(object), "object"},
		{typeof(void), "void"}
	};

	private static Func<Type, object>? _getUninitializedObject;

	public static bool IsAssignableFrom(this Type type, Type otherType)
	{
		return type.GetTypeInfo().IsAssignableFrom(otherType.GetTypeInfo());
	}

	public static ConstructorInfo? FindConstructor(this Type type, Type[] parameterTypes)
	{
		foreach (var constructor in type.GetTypeInfo().DeclaredConstructors)
		{
			if (Methods.ParametersMatch(constructor.GetParameters(), parameterTypes))
				return constructor;
		}

		return default;
	}

	private static bool TypesMatch(Type[] a, Type[] b)
	{
		if (a.Length != b.Length)
			return false;

		for (var i = 0; i < a.Length; i++)
		{
			if (a[i] != b[i])
				return false;
		}

		return true;
	}

	public static IEnumerable<TypeInfo> GetInheritedTypeInfos(this Type type)
	{
		var info = type.GetTypeInfo();

		yield return info;

		if (info.IsInterface)
		{
			foreach (var ii in info.ImplementedInterfaces)
			{
				foreach (var iface in ii.GetInheritedTypeInfos())
					yield return iface;
			}
		}
		else
		{
			for (var i = info.BaseType?.GetTypeInfo(); i != null; i = i.BaseType?.GetTypeInfo())
				yield return i;
		}
	}

	public static object GetUninitializedObject(this Type type)
	{
		if (_getUninitializedObject is null)
		{
			var a = typeof(System.Runtime.CompilerServices.RuntimeHelpers).GetTypeInfo().Assembly;
			var fs = a.DefinedTypes.FirstOrDefault(t => string.Equals(t.FullName, "System.Runtime.Serialization.FormatterServices"));
			var guo = fs?.DeclaredMethods.FirstOrDefault(m => m.Name == nameof(GetUninitializedObject));

			if (guo is null)
				throw new NotSupportedException($"The runtime does not support the '{nameof(GetUninitializedObject)}' API.");

			Interlocked.CompareExchange(ref _getUninitializedObject, (Func<Type, object>)guo.CreateDelegate(typeof(Func<Type, object>)), null);
		}

		return type.GetUninitializedObject();
	}

	public static bool IsTypePrimitive(this Type type)
	{
		if (type == null)
			return false;

		return type.IsPrimitive
					|| type == typeof(string)
					|| type == typeof(decimal)
					|| type.IsEnum
					|| type.IsValueType;
	}

	public static bool IsNumber(this Type type)
	{
		return type == typeof(byte)
			|| type == typeof(sbyte)
			|| type == typeof(short)
			|| type == typeof(ushort)
			|| type == typeof(int)
			|| type == typeof(uint)
			|| type == typeof(long)
			|| type == typeof(ulong)
			|| type == typeof(float)
			|| type == typeof(double)
			|| type == typeof(decimal);
	}

	public static string ShortName(this Type type)
	{
		var r = type.Name;

		if (r.Contains('.'))
			r = r.Substring(r.LastIndexOf('.') + 1);

		return r;
	}

	public static object? CreateInstance(this Type type)
	{
		return CreateInstance(type, null);
	}

	public static object? CreateInstance(this Type type, object[]? ctorArgs)
	{
		if (type is null)
			return default;

		object? instance;

		if (ctorArgs is null)
			instance = CreateInstanceInternal(type);
		else
			instance = CreateInstanceInternal(type, BindingFlags.CreateInstance, ctorArgs);

		return instance;
	}

	public static T? CreateInstance<T>(this Type type)
	{
		return CreateInstance<T>(type, null);
	}

	public static T? CreateInstance<T>(this Type type, object[]? ctorArgs)
	{
		if (type is null)
			return default;

		object? instance = null;

		if (ctorArgs is null)
			instance = CreateInstanceInternal(type);
		else
			instance = CreateInstanceInternal(type, BindingFlags.CreateInstance, ctorArgs);

		if (instance is null)
			throw new NullReferenceException($"{Strings.ErrInvalidInterface} ({typeof(T).Name})");

		return (T?)Convert.ChangeType(instance, typeof(T?));
	}

	private static object? CreateInstanceInternal(this Type type)
	{
		if (type.IsTypePrimitive())
			return type.GetDefault();

		return Activator.CreateInstance(type);
	}

	private static object? CreateInstanceInternal(this Type type, BindingFlags bindingFlags, object[] ctorArgs)
	{
		if (type.IsTypePrimitive())
			return type.GetDefault();

		return Activator.CreateInstance(type, bindingFlags, null, ctorArgs, CultureInfo.InvariantCulture);
	}

	public static object? GetDefault(this Type type)
	{
		if (type is null || type == typeof(void))
			return default;

		var isNullable = !type.GetTypeInfo().IsValueType || type.IsNullable();

		if (isNullable)
			return default;

		if (type.ContainsGenericParameters)
			throw new NullReferenceException($"{MethodBase.GetCurrentMethod()} {Strings.ErrDefaultGeneric} ({type})");

		return Activator.CreateInstance(type);
	}

	public static Type? ResolveInterface(object component, string method, params Type[] parameters)
	{
		var itfs = component.GetType().GetInterfaces();

		foreach (var i in itfs)
		{
			var methods = i.GetMethods(BindingFlags.Public | BindingFlags.Instance).Where(f => string.Equals(f.Name, method, StringComparison.Ordinal));

			foreach (var m in methods)
			{
				var pars = m.GetParameters();

				if (pars.Length != parameters.Length)
					continue;

				var parametersMatch = true;

				for (var j = 0; j < parameters.Length; j++)
				{
					if (pars[j].ParameterType != parameters[j] && !parameters[j].IsAssignableFrom(pars[j].ParameterType))
					{
						parametersMatch = false;
						break;
					}
				}

				if (parametersMatch)
					return i;
			}
		}

		return null;
	}

	public static Type? GetType(string type)
	{
		if (AliasedTypes.ContainsValue(type))
			return AliasedTypes.First(f => string.Equals(f.Value, type, StringComparison.Ordinal)).Key;

		var t = Type.GetType(type, false);

		if (t is not null)
			return t;

		var asm = LoadAssembly(type);

		if (asm is null)
			return null;

		var typeName = type.Split(',')[0];

		return asm.GetType(typeName);
	}

	public static Assembly? LoadAssembly(string type)
	{
		var tokens = type.Split(',');
		var libraryName = string.Empty;
		var path = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);

		if (path is null)
			return null;

		if (tokens.Length == 1)
			libraryName = $"{tokens[0].Trim()}.dll";
		else if (tokens.Length > 1)
			libraryName = $"{tokens[1].Trim()}.dll";

		var file = Path.Combine(path, libraryName);

		if (!File.Exists(file))
			return null;

		return AssemblyLoadContext.Default.LoadFromAssemblyName(AssemblyName.GetAssemblyName(file));
	}

	public static T? FindAttribute<T>(this Type type) where T : Attribute
	{
		var atts = type.GetCustomAttributes<T>(true);

		if (atts is null || !atts.Any())
			return default;

		return atts.ElementAt(0);
	}

	public static List<T> FindAttributes<T>(this Type type) where T : Attribute
	{
		var r = type.GetCustomAttributes<T>(true);

		if (r is null)
			return [];

		return r.ToList();
	}

	public static string ToFriendlyName(this Type type)
	{
		if (AliasedTypes.ContainsKey(type))
			return AliasedTypes[type];
		else if (type.IsGenericType)
			return type.Name.Split('`')[0] + "<" + string.Join(", ", type.GetGenericArguments().Select(x => ToFriendlyName(x)).ToArray()) + ">";
		else
			return type.ShortName();
	}
}
