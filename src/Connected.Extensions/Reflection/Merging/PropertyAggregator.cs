using Connected.Services;
using System.Collections;

namespace Connected.Reflection.Merging;

internal static class PropertyAggregator
{
	public static Dictionary<string, object?> Aggregate(params object?[] values)
	{
		var result = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);

		for (var i = values.Length - 1; i >= 0; i--)
		{
			if (values[i] is not object value)
				continue;

			foreach (var property in GetImplementedProperties(value))
			{
				if (result.ContainsKey(property.Key))
					continue;

				result.Add(property.Key, property.Value);
			}

			if (value is IPropertyProvider provider)
			{
				foreach (var property in provider.Properties)
				{
					if (result.ContainsKey(property.Key))
						continue;

					result.Add(property.Key, property.Value);
				}
			}
		}

		return result;
	}

	private static Dictionary<string, object?> GetImplementedProperties(object value)
	{
		var result = new Dictionary<string, object?>();

		if (value is null)
			return result;

		if (value is object[] objectArray)
		{
			for (var i = objectArray.Length - 1; i >= 0; i--)
			{
				var implementations = Properties.GetImplementedProperties(objectArray[i]);

				foreach (var property in implementations)
					result.Add(property.Name, objectArray[i]);
			}
		}
		else if (value.GetType().IsDictionary())
		{
			if (value is not IDictionary dictionary)
				return result;

			foreach (var item in dictionary.Keys)
			{
				var key = item.ToString();

				if (key is null)
					continue;

				result.Add(key, dictionary[item]);
			}
		}
		else
		{
			var implementations = Properties.GetImplementedProperties(value);

			foreach (var property in implementations)
				result.Add(property.Name, value);
		}

		return result;
	}
}
