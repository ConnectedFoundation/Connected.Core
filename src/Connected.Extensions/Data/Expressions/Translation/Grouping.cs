using System.Collections;

namespace Connected.Data.Expressions.Translation;

internal sealed class Grouping<TKey, TElement>(TKey key, IEnumerable<TElement> group)
	: IGrouping<TKey, TElement>
{
	public TKey Key { get; } = key;

	public IEnumerator<TElement> GetEnumerator()
	{
		if (group is not List<TElement>)
			group = [.. group];

		return group.GetEnumerator();
	}

	IEnumerator IEnumerable.GetEnumerator()
	{
		return group.GetEnumerator();
	}
}