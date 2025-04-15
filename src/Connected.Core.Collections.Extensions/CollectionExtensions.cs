using Connected.Annotations;
using System.Collections;
using System.Collections.Immutable;
using System.Reflection;

namespace Connected.Collections;

public static class CollectionExtensions
{
	public static Dictionary<string, string?> ToDictionary(this IEnumerable items, string keyProperty, string valueProperty)
	{
		var result = new Dictionary<string, string?>();

		foreach (var item in items)
		{
			var keyPropertyInfo = item.GetType().GetProperty(keyProperty);
			var valuePropertyInfo = item.GetType().GetProperty(valueProperty);

			if (keyPropertyInfo is null)
				throw new NullReferenceException($"{Strings.ErrPropertyNotFound} ('{keyProperty}')");

			if (valuePropertyInfo is null)
				throw new NullReferenceException($"{Strings.ErrPropertyNotFound} ('{valueProperty}')");

			var keyValue = keyPropertyInfo.GetValue(item)?.ToString();
			var valueValue = valuePropertyInfo.GetValue(item)?.ToString();

			if (keyValue is null)
				continue;

			result.Add(keyValue, valueValue);
		}

		return result;
	}

	public static ImmutableArray<TSource> ToImmutableArray<TSource>(this IEnumerable<TSource> items, bool performLock)
	{
		if (!performLock)
			return items.ToImmutableArray();

		lock (items)
			return items.ToImmutableArray();
	}

	public static IImmutableList<TSource> ToImmutableList<TSource>(this IEnumerable<TSource> items, bool performLock)
	{
		if (!performLock)
			return items.ToImmutableList();

		lock (items)
			return items.ToImmutableList();
	}

	public static void SortByOrdinal<TElement>(this List<TElement> items)
	{
		items.Sort((left, right) =>
		{
			var leftOrdinal = left is Type lt ? lt.GetCustomAttribute<OrdinalAttribute>() : left?.GetType().GetCustomAttribute<OrdinalAttribute>();
			var rightOrdinal = right is Type rt ? rt.GetCustomAttribute<OrdinalAttribute>() : right?.GetType().GetCustomAttribute<OrdinalAttribute>();

			if (leftOrdinal is null && rightOrdinal is null)
				return 0;

			if (leftOrdinal is not null && rightOrdinal is null)
				return -1;

			if (leftOrdinal is null && rightOrdinal is not null)
				return 1;

			if (leftOrdinal?.Ordinal == rightOrdinal?.Ordinal)
				return 0;
			else if (leftOrdinal?.Ordinal < rightOrdinal?.Ordinal)
				return 1;
			else
				return -1;
		});
	}
}