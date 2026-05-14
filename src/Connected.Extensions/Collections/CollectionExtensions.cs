using Connected.Annotations;
using System.Collections;
using System.Collections.Immutable;
using System.Reflection;

namespace Connected.Collections;

/// <summary>
/// Provides extension methods for collection transformation, ordering, and immutable access patterns.
/// </summary>
public static class CollectionExtensions
{
	/// <summary>
	/// Converts a non-generic enumerable into a dictionary by resolving key and value members using reflection.
	/// </summary>
	/// <param name="items">The source item sequence.</param>
	/// <param name="keyProperty">The property name used as dictionary key.</param>
	/// <param name="valueProperty">The property name used as dictionary value.</param>
	/// <returns>A dictionary created from resolved key and value property values.</returns>
	/// <exception cref="NullReferenceException">
	/// Thrown when either <paramref name="keyProperty"/> or <paramref name="valueProperty"/> is not found on an item.
	/// </exception>
	/// <exception cref="ArgumentException">
	/// Thrown when duplicate keys are encountered while adding entries to the dictionary.
	/// </exception>
	public static Dictionary<string, string?> ToDictionary(this IEnumerable items, string keyProperty, string valueProperty)
	{
		var result = new Dictionary<string, string?>();

		/*
		 * Iterate through source items and map each item to a key-value pair by reading
		 * the configured property names from the runtime type.
		 */
		foreach (var item in items)
		{
			var keyPropertyInfo = item.GetType().GetProperty(keyProperty);

			var valuePropertyInfo = item.GetType().GetProperty(valueProperty);

			/*
			 * Ensure the key member exists before continuing with value extraction.
			 */
			if (keyPropertyInfo is null)
				throw new NullReferenceException($"{Strings.ErrPropertyNotFound} ('{keyProperty}')");

			/*
			 * Ensure the value member exists before continuing with value extraction.
			 */
			if (valuePropertyInfo is null)
				throw new NullReferenceException($"{Strings.ErrPropertyNotFound} ('{valueProperty}')");

			var keyValue = keyPropertyInfo.GetValue(item)?.ToString();

			var valueValue = valuePropertyInfo.GetValue(item)?.ToString();

			/*
			 * Skip entries that do not resolve a key because dictionary keys must be non-null.
			 */
			if (keyValue is null)
				continue;

			/*
			 * Add the resolved key and value to the resulting dictionary.
			 */
			result.Add(keyValue, valueValue);
		}

		/*
		 * Return the fully materialized dictionary.
		 */
		return result;
	}

	/// <summary>
	/// Creates an immutable array from the source sequence, optionally synchronizing access on the source instance.
	/// </summary>
	/// <typeparam name="TSource">The source element type.</typeparam>
	/// <param name="items">The source sequence.</param>
	/// <param name="performLock">Indicates whether source access should be synchronized with a lock.</param>
	/// <returns>An immutable array containing source elements.</returns>
	public static ImmutableArray<TSource> ToImmutableArray<TSource>(this IEnumerable<TSource> items, bool performLock)
	{
		/*
		 * Materialize directly when synchronization is not requested.
		 */
		if (!performLock)
			return [.. items];

		/*
		 * Materialize under lock to prevent concurrent source mutation during enumeration.
		 */
		lock (items)
			return [.. items];
	}

	/// <summary>
	/// Creates an immutable list from the source sequence, optionally synchronizing access on the source instance.
	/// </summary>
	/// <typeparam name="TSource">The source element type.</typeparam>
	/// <param name="items">The source sequence.</param>
	/// <param name="performLock">Indicates whether source access should be synchronized with a lock.</param>
	/// <returns>An immutable list containing source elements.</returns>
	public static IImmutableList<TSource> ToImmutableList<TSource>(this IEnumerable<TSource> items, bool performLock)
	{
		/*
		 * Materialize directly when synchronization is not requested.
		 */
		if (!performLock)
			return items.ToImmutableList();

		/*
		 * Materialize under lock to prevent concurrent source mutation during enumeration.
		 */
		lock (items)
			return items.ToImmutableList();
	}

	/// <summary>
	/// Sorts elements by <see cref="OrdinalAttribute"/> in descending ordinal order.
	/// </summary>
	/// <typeparam name="TElement">The element type to sort.</typeparam>
	/// <param name="items">The list to sort in-place.</param>
	public static void SortByOrdinal<TElement>(this List<TElement> items)
	{
		/*
		 * Use attribute metadata to determine sort precedence. Elements with an ordinal
		 * are prioritized over elements without one.
		 */
		items.Sort((left, right) =>
		{
			var leftOrdinal = left is Type lt ? lt.GetCustomAttribute<OrdinalAttribute>() : left?.GetType().GetCustomAttribute<OrdinalAttribute>();

			var rightOrdinal = right is Type rt ? rt.GetCustomAttribute<OrdinalAttribute>() : right?.GetType().GetCustomAttribute<OrdinalAttribute>();

			/*
			 * Preserve relative order when neither side provides ordinal metadata.
			 */
			if (leftOrdinal is null && rightOrdinal is null)
				return 0;

			/*
			 * Prioritize elements that explicitly declare ordinal metadata.
			 */
			if (leftOrdinal is not null && rightOrdinal is null)
				return -1;

			if (leftOrdinal is null && rightOrdinal is not null)
				return 1;

			/*
			 * Compare ordinal numeric values in descending order.
			 */
			if (leftOrdinal?.Ordinal == rightOrdinal?.Ordinal)
				return 0;

			else if (leftOrdinal?.Ordinal < rightOrdinal?.Ordinal)
				return 1;

			else
				return -1;
		});
	}

	/// <summary>
	/// Returns the first entity in an immutable list.
	/// </summary>
	/// <typeparam name="T">The element type.</typeparam>
	/// <param name="entities">The immutable list to inspect.</param>
	/// <returns>The first element when available; otherwise <see langword="default"/>.</returns>
	public static T? FirstEntity<T>(this IImmutableList<T> entities)
	{
		/*
		 * Return default when no items are available.
		 */
		if (entities.Count == 0)
			return default;

		/*
		 * Return the first item as the selected entity.
		 */
		return entities[0];
	}
}