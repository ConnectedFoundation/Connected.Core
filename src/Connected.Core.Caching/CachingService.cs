using System.Collections.Immutable;

namespace Connected.Caching;

internal sealed class CachingService : InProcessCache, ICachingService, IDisposable
{
	protected override async Task OnRemove(string key, object id)
	{
		await base.OnRemove(key, id);

		var ids = new List<string>();

		if (id is not null)
			ids.Add(ResolveId(id));

		await OnRemove(key, ids.ToImmutableList());
	}
}