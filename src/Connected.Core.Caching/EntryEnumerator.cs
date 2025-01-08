using Connected.Reflection;
using System.Collections;
using System.Collections.Concurrent;

namespace Connected.Caching;

internal class EntryEnumerator<T>(ConcurrentDictionary<string, IEntry> items) : IEnumerator<T>
{
	private int Count => items.Count;
	private int Index { get; set; } = -1;

	object IEnumerator.Current => Current;

	public T Current
	{
		get
		{
			var item = items.ElementAt(Index);

			if (item.Value is null || item.Value.Instance is null)
				throw new NullReferenceException();

			return Types.Convert<T>(item.Value.Instance) ?? throw new NullReferenceException();
		}
	}

	public void Dispose()
	{
	}

	public bool MoveNext()
	{
		if (Index < Count - 1)
		{
			Index++;

			return true;
		}

		return false;
	}

	public void Reset()
	{
		Index = -1;
	}
}