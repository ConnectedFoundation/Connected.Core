using Connected.Reflection;
using Google.Protobuf.Collections;
using System.Collections;
using System.Reflection;

namespace Connected.Net.Grpc;
internal static class GrpcConverter
{
	public static TReturnValue Convert<TReturnValue>(object? value)
	{
		if (value is null)
			return Activator.CreateInstance<TReturnValue>();

		if (value.GetType().IsEnumerable())
			return SerializeEnumerable<TReturnValue>(value);
		else if (value.GetType().IsTypePrimitive())
			return SerializePrimitive<TReturnValue>(value);

		var instance = Activator.CreateInstance<TReturnValue>();

		return Serializer.Merge(instance, value);
	}

	private static TReturnValue SerializePrimitive<TReturnValue>(object value)
	{
		var properties = typeof(TReturnValue).GetProperties(BindingFlags.Public | BindingFlags.Instance | BindingFlags.SetProperty);

		if (properties.Length != 1)
			return Activator.CreateInstance<TReturnValue>();

		var property = properties[0].Name;
		var instance = Activator.CreateInstance<TReturnValue>();

		if (instance is null)
			return Activator.CreateInstance<TReturnValue>();

		typeof(TReturnValue).GetProperty(property)?.SetValue(instance, value);

		return instance;
	}

	private static TReturnValue SerializeEnumerable<TReturnValue>(object value)
	{
		var en = (IEnumerable)value;

		if (en is null)
			return Activator.CreateInstance<TReturnValue>();

		var elementType = ResolveElementType<TReturnValue>();

		if (elementType is null)
			return Activator.CreateInstance<TReturnValue>();

		var enumerator = en.GetEnumerator();
		var result = new List<object?>();

		while (enumerator.MoveNext())
		{
			var item = enumerator.Current;

			if (item is null)
				result.Add(item);

			var instance = Activator.CreateInstance(elementType);

			result.Add(Serializer.Merge(instance, item));
		}

		var rv = Activator.CreateInstance<TReturnValue>();

		if (result.Count > 0)
			AddRange(rv, elementType, result);

		return rv;
	}

	private static Type? ResolveElementType<TReturnValue>()
	{
		return ResolveItemsProperty<TReturnValue>()?.PropertyType.GetGenericArguments().FirstOrDefault();
	}

	private static PropertyInfo? ResolveItemsProperty<TReturnValue>()
	{
		return typeof(TReturnValue).GetProperty("Items", BindingFlags.Public | BindingFlags.Instance | BindingFlags.GetProperty);
	}

	private static void AddRange<TReturnValue>(TReturnValue instance, Type elementType, List<object?> items)
	{
		if (items.Count == 0)
			return;

		var itemsProperty = ResolveItemsProperty<TReturnValue>();

		if (itemsProperty is null)
			return;

		var addRangeMethod = itemsProperty.PropertyType.GetMethod(nameof(RepeatedField<object>.AddRange), BindingFlags.Public | BindingFlags.Instance | BindingFlags.InvokeMethod);

		if (addRangeMethod is null)
			return;

		var arr = Array.CreateInstance(elementType, items.Count);

		for (var i = 0; i < items.Count; i++)
			arr.SetValue(items[i], i);

		var property = itemsProperty.GetValue(instance);

		addRangeMethod.Invoke(property, [arr]);
	}
}
