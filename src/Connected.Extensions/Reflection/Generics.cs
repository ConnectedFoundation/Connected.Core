namespace Connected.Reflection;

public static class Generics
{
	public static bool IsSubclassOfGenericType(this Type type, Type genericType)
	{
		var current = type.BaseType;

		while (current is not null && current != typeof(object))
		{
			var currentType = current.IsGenericType ? current.GetGenericTypeDefinition() : current;

			if (genericType == currentType)
				return true;

			current = current.BaseType;
		}

		return false;
	}

	public static bool ImplementsInterface(this Type type, Type interfaceType)
	{
		var interfaces = type.GetInterfaces();

		foreach (var i in interfaces)
		{
			if (!i.IsGenericType)
			{
				if (i == interfaceType)
					return true;

				continue;
			}

			var definition = i.GetGenericTypeDefinition();

			if (definition == interfaceType)
				return true;
		}

		return false;
	}

	public static bool ImplementsInterface<TInterface>(this Type type)
	{
		return ImplementsInterface(type, typeof(TInterface));
	}
}
