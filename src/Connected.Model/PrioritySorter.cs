using Connected.Annotations;
using System.Reflection;

namespace Connected;

public static class PrioritySorter
{
	public static void SortByPriority<TElement>(this List<TElement> items)
	{
		items.Sort((left, right) =>
		{
			var leftPriority = left is Type lt ? lt.GetCustomAttribute<PriorityAttribute>() : left?.GetType().GetCustomAttribute<PriorityAttribute>();
			var rightPriority = right is Type rt ? rt.GetCustomAttribute<PriorityAttribute>() : right?.GetType().GetCustomAttribute<PriorityAttribute>();

			if (leftPriority is null && rightPriority is null)
				return 0;

			if (leftPriority is not null && rightPriority is null)
				return -1;

			if (leftPriority is null && rightPriority is not null)
				return 1;

			if (leftPriority?.Priority == rightPriority?.Priority)
				return 0;
			else if (leftPriority?.Priority > rightPriority?.Priority)
				return -1;
			else
				return 1;
		});
	}
}
