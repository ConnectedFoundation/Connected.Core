using Connected.Annotations;
using System.Reflection;

namespace Connected.Collections;

/// <summary>
/// Provides extension methods for sorting collections based on priority attributes.
/// </summary>
/// <remarks>
/// This static class enables sorting of list elements by examining their PriorityAttribute
/// metadata. Items with higher priority values are placed first in the sorted collection.
/// If elements lack priority attributes, they are placed after those with attributes.
/// The sorting algorithm handles both Type objects and instance objects, extracting
/// priority metadata from the appropriate reflection target.
/// </remarks>
public static class PrioritySorter
{
	/// <summary>
	/// Sorts a list of elements in-place based on their priority attribute values.
	/// </summary>
	/// <typeparam name="TElement">The type of elements in the list.</typeparam>
	/// <param name="items">The list to sort by priority.</param>
	/// <remarks>
	/// Elements are sorted in descending priority order (highest priority first).
	/// If an element is a Type, its priority attribute is examined directly.
	/// If an element is an instance, its runtime type's priority attribute is examined.
	/// Elements without priority attributes are placed after those with attributes.
	/// Elements with equal priorities maintain their relative order (stable sort behavior depends on Sort implementation).
	/// </remarks>
	public static void SortByPriority<TElement>(this List<TElement> items)
	{
		/*
		 * Sort the list using a custom comparison that evaluates priority attributes.
		 * The comparison extracts priority metadata from each element and determines
		 * their relative ordering based on the following rules:
		 * 1. Elements with priority attributes come before those without
		 * 2. Among elements with priorities, higher values come first
		 * 3. Elements without priorities are considered equal
		 */
		items.Sort((left, right) =>
		{
			/*
			 * Extract priority attributes from both elements.
			 * If the element is a Type, get the attribute from the Type itself.
			 * Otherwise, get the attribute from the element's runtime type.
			 */
			var leftPriority = left is Type lt ? lt.GetCustomAttribute<PriorityAttribute>() : left?.GetType().GetCustomAttribute<PriorityAttribute>();
			var rightPriority = right is Type rt ? rt.GetCustomAttribute<PriorityAttribute>() : right?.GetType().GetCustomAttribute<PriorityAttribute>();

			/*
			 * Handle the case where neither element has a priority attribute.
			 * These elements are considered equal in terms of priority.
			 */
			if (leftPriority is null && rightPriority is null)
				return 0;

			/*
			 * Handle the case where only the left element has a priority attribute.
			 * Elements with priorities come before those without, so left comes first.
			 */
			if (leftPriority is not null && rightPriority is null)
				return -1;

			/*
			 * Handle the case where only the right element has a priority attribute.
			 * Elements with priorities come before those without, so right comes first.
			 */
			if (leftPriority is null && rightPriority is not null)
				return 1;

			/*
			 * Compare the actual priority values when both elements have attributes.
			 * Higher priority values should come first (descending order).
			 */
			if (leftPriority?.Priority == rightPriority?.Priority)
				return 0;
			else if (leftPriority?.Priority > rightPriority?.Priority)
				return -1;
			else
				return 1;
		});
	}
}
