using Connected.Storage.Transactions;
using System.Collections.Immutable;

namespace Connected.Caching;

internal sealed class CachingService(ITransactionContext transactions)
	: InProcessCache, ICachingService, IDisposable
{
	public ICacheContext CreateContext()
	{
		return new CacheContext(this, transactions);
	}

	protected override async Task OnRemove(string key, object id)
	{
		await base.OnRemove(key, id);

		var ids = new List<string>();

		if (id is not null)
			ids.Add(ResolveId(id));

		await OnRemove(key, ids.ToImmutableList());
	}
}