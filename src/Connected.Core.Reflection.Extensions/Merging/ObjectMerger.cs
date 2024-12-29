using System.Collections;
using System.Reflection;

namespace Connected.Reflection.Merging;

internal sealed class ObjectMerger : Merger
{
	public void Merge(object destination, params object?[] sources)
	{
		if (destination is null || !HasSource(sources))
			return;

		var sourceProperties = PropertyAggregator.Aggregate(sources);

		foreach (var property in Properties.GetImplementedProperties(destination))
			MergeProperty(destination, sourceProperties, property);
	}

	private static bool HasSource(params object?[] sources)
	{
		foreach (var source in sources)
		{
			if (source is not null)
				return true;
		}

		return false;
	}

	private void MergeProperty(object destination, Dictionary<string, object?> sourceProperties, PropertyInfo property)
	{
		if (property.PropertyType.IsTypePrimitive())
		{
			if (!property.CanWrite)
				return;

			if (!sourceProperties.TryGetValue(property.Name, out object? source))
				return;

			if (source is null)
				property.SetValue(destination, null);
			else if (source.GetType() is Type propertyType && PropertyResolver.Resolve(propertyType, property.Name) is PropertyInfo propertyInfo)
				property.SetValue(destination, propertyInfo.GetValue(source));
			else
			{
				var converted = Convert.ChangeType(source, property.PropertyType);

				property.SetValue(destination, converted);
			}
		}
		else if (IsArray(property))
			MergeEnumerable(destination, sourceProperties, property);
		else
			MergeObject(destination, sourceProperties, property);
	}

	private void MergeEnumerable(object destination, Dictionary<string, object?> sourceProperties, PropertyInfo property)
	{
		if (!sourceProperties.TryGetValue(property.Name, out object? sourceProperty) || sourceProperty is null)
			return;

		var targetProperty = sourceProperty.GetType().GetProperty(property.Name);
		object? targetValue = null;

		if (targetProperty is null)
		{
			if (sourceProperty.GetType().IsEnumerable())
				targetValue = sourceProperty;
		}
		else
			targetValue = targetProperty.GetValue(sourceProperty);

		if (targetValue is null)
			return;

		if (!targetValue.GetType().IsArray && targetValue is not IEnumerable)
			return;

		var sourceElements = GetElements(targetValue);
		var destinationValue = property.GetValue(destination);

		if (destinationValue is null && !property.CanWrite)
			return;

		var addMethod = property.PropertyType.GetMethod(nameof(IList.Add), BindingFlags.Public | BindingFlags.Instance | BindingFlags.NonPublic);
		object? instance = null;

		if (addMethod is not null)
			instance = Activator.CreateInstance(property.PropertyType);
		else
		{
			var arrayType = property.PropertyType.GetElementType();

			if (arrayType is not null)
				instance = Array.CreateInstance(arrayType, sourceElements.Count);
		}

		if (instance is null)
			return;

		var elementType = instance.GetType().IsArray
			? instance.GetType().GetElementType()
		 	: instance.GetType().GetGenericArguments()?[0];

		if (elementType is null)
			return;

		for (var i = 0; i < sourceElements.Count; i++)
		{
			/*
			 * TODO: handle Dictionary
			 */
			var sourceElement = sourceElements[i];
			object? item;

			if (elementType.IsTypePrimitive())
				item = Convert.ChangeType(sourceElement, elementType);
			else
			{
				item = Activator.CreateInstance(elementType);

				if (item is null)
					continue;

				Merge(item, sourceElement);
			}

			if (addMethod is not null)
				addMethod.Invoke(instance, [item]);
			else
				((Array)instance).SetValue(item, i);
		}

		property.SetValue(destination, instance);
	}

	private static void MergeObject(object destination, Dictionary<string, object?> source, PropertyInfo property)
	{
		if (!property.CanWrite)
			throw new NotImplementedException("Deep merge is not implemented.");

		if (!source.TryGetValue(property.Name, out object? value) || value is null)
			return;

		var propertyValue = value;
		var sourceProperty = value.GetType().GetProperty(property.Name);

		if (sourceProperty is not null)
			propertyValue = sourceProperty.GetValue(value);

		property.SetValue(destination, propertyValue);
	}
}