using Connected.Services;
using System.Collections.Immutable;

namespace Connected.Storage.Sql;

internal sealed class SqlStorageConnectionProvider(ICancellationContext cancel)
		: StorageConnectionProvider
{
	private ICancellationContext Cancel { get; } = cancel;

	protected override async Task<IImmutableList<IStorageConnection>> OnInvoke<TEntity>()
	{
		return await Task.FromResult(new List<IStorageConnection> { new SqlDataConnection(Cancel) }.ToImmutableList());
	}
}
