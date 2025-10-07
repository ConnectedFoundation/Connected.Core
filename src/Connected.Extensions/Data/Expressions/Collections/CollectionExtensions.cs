using System.Collections.ObjectModel;

namespace Connected.Data.Expressions.Collections;

internal static class Extensions
{
	public static ReadOnlyCollection<T> ToReadOnly<T>(this IEnumerable<T> sequence)
	{
		if (sequence is not ReadOnlyCollection<T> collection)
		{
			if (sequence is null)
				collection = EmptyReadOnlyCollection<T>.Empty;
			else
				collection = new List<T>(sequence).AsReadOnly();
		}

		return collection;
	}

	private class EmptyReadOnlyCollection<T>
	{
		public static readonly ReadOnlyCollection<T> Empty = new List<T>().AsReadOnly();
	}
}