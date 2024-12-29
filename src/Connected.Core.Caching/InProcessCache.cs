using System.Collections.Immutable;

namespace Connected.Caching;

internal class InProcessCache : Cache, IInProcessCache
{
	public void Merge(ICache cache)
	{
		if (cache.Keys() is not ImmutableList<string> keys)
			return;

		foreach (var key in keys)
		{
			if (cache.Ids(key) is not ImmutableList<string> entryKeys)
				continue;

			foreach (var entryKey in entryKeys)
			{
				if (cache.Get(key, entryKey) is IEntry entry)
					CopyTo(key, entryKey, entry);
			}
		}
	}
}