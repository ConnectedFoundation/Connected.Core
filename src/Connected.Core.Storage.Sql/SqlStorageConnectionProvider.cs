using Connected.Services;
using System.Collections.Immutable;

namespace Connected.Storage.Sql;

internal sealed class SqlStorageConnectionProvider : StorageConnectionProvider
{
	public SqlStorageConnectionProvider(ICancellationContext cancel)
	{
		Cancel = cancel;
	}

	private ICancellationContext Cancel { get; }

	protected override Task<ImmutableList<IStorageConnection>> OnInvoke<TEntity>()
	{
		return Task.FromResult(new List<IStorageConnection> { new SqlDataConnection(Cancel) }.ToImmutableList());
	}
}
