using Connected.Annotations.Entities;
using Connected.Reflection;
using System.Data;
using System.Reflection;

namespace Connected.Storage;

internal static class ReturnValueBinder
{
	public static void Bind(IStorageOperation operation, object entity)
	{
		if (operation is null || operation.Parameters is null)
			return;

		foreach (var parameter in operation.Parameters)
		{
			if (!parameter.Direction.HasFlag(ParameterDirection.Output))
				continue;

			if (string.IsNullOrWhiteSpace(parameter.Name))
				continue;

			if (parameter.Value == DBNull.Value)
				continue;

			var properties = new List<PropertyInfo>();

			var all = entity.GetType().GetProperties(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);

			foreach (var prop in all)
			{
				if (prop.FindAttribute<ReturnValueAttribute>() is not null)
					properties.Add(prop);
			}

			PropertyInfo? property = null;

			foreach (var prop in properties)
			{
				if (string.Equals(prop.Name, parameter.Name, StringComparison.Ordinal))
				{
					property = prop;
					break;
				}
			}

			if (property is null)
			{
				foreach (var prop in properties)
				{
					if (string.Equals(prop.Name, parameter.Name, StringComparison.OrdinalIgnoreCase))
					{
						property = prop;
						break;
					}
				}
			}

			if (property is null)
			{
				var candidates = new List<string>
				{
					parameter.Name.Replace("@", string.Empty)
				};

				foreach (var prop in properties)
				{
					foreach (var candidate in candidates)
					{
						if (string.Equals(prop.Name, candidate, StringComparison.OrdinalIgnoreCase))
						{
							property = prop;
							break;
						}
					}

					if (property is not null)
						break;
				}
			}

			if (property is null)
				continue;

			var existingValue = property.GetValue(entity);
			var overwriteAtt = property.FindAttribute<ReturnValueAttribute>();

			if (overwriteAtt is null)
				continue;

			property.SetValue(entity, Types.Convert(parameter.Value, property.PropertyType));
		}
	}
}
