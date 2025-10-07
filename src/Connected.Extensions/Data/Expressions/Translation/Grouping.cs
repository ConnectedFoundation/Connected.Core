using System.Linq;
using System.Collections;
using System.Collections.Generic;

namespace Connected.Data.Expressions.Translation;

internal sealed class Grouping<TKey, TElement> : IGrouping<TKey, TElement>
{
	public Grouping(TKey key, IEnumerable<TElement> group)
	{
		Key = key;
		Group = group;
	}

	public TKey Key { get; }
	private IEnumerable<TElement> Group { get; set; }

	public IEnumerator<TElement> GetEnumerator()
	{
		if (!(Group is List<TElement>))
			Group = Group.ToList();

		return Group.GetEnumerator();
	}

	IEnumerator IEnumerable.GetEnumerator()
	{
		return Group.GetEnumerator();
	}
}