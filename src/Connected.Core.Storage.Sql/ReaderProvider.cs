using Connected.Annotations;

namespace Connected.Storage.Sql;

[Priority(0)]
internal sealed class ReaderProvider : StorageReaderProvider
{
	protected override Task<IStorageReader<TEntity>?> OnInvoke<TEntity>()
	{
		return Task.FromResult<IStorageReader<TEntity>?>(new DatabaseReader<TEntity>(Operation, Connection));
	}
}