using System.Collections.Generic;
namespace Connected.Data.Expressions.Collections;

internal sealed class ScopedDictionary<TKey, TValue>
	where TKey : notnull
{
	public ScopedDictionary(ScopedDictionary<TKey, TValue>? previous)
	{
		Previous = previous;
		Map = new();
	}

	public ScopedDictionary(ScopedDictionary<TKey, TValue>? previous, IEnumerable<KeyValuePair<TKey, TValue>> pairs)
		 : this(previous)
	{
		foreach (var p in pairs)
			Map.Add(p.Key, p.Value);
	}

	private ScopedDictionary<TKey, TValue>? Previous { get; }
	private Dictionary<TKey, TValue> Map { get; }

	public void Add(TKey key, TValue value)
	{
		Map.Add(key, value);
	}

	public bool TryGetValue(TKey key, out TValue? value)
	{
		for (var scope = this; scope is not null; scope = scope.Previous)
		{
			if (scope.Map.TryGetValue(key, out value))
				return true;
		}

		value = default;

		return false;
	}

	public bool ContainsKey(TKey key)
	{
		for (var scope = this; scope is not null; scope = scope.Previous)
		{
			if (scope.Map.ContainsKey(key))
				return true;
		}

		return false;
	}
}