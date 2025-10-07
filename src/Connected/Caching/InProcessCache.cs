using System.Collections.Immutable;

namespace Connected.Caching;

internal class InProcessCache : Cache, IInProcessCache
{
	public void Merge(ICacheContext cache)
	{
		if (cache.OwnKeys() is not IImmutableList<string> keys)
			return;

		foreach (var key in keys)
		{
			if (cache.OwnIds(key) is not IImmutableList<string> entryKeys)
				continue;

			foreach (var entryKey in entryKeys)
			{
				if (cache.Get(key, entryKey) is IEntry entry)
					CopyTo(key, entryKey, entry);
			}
		}
	}
}