using Connected.Annotations;

namespace Connected.Storage.Sql;

[Priority(0)]
internal sealed class WriterProvider
	: StorageWriterProvider
{
	protected override Task<IStorageWriter?> OnInvoke<TEntity>()
	{
		return Task.FromResult<IStorageWriter?>(new DatabaseWriter(Operation, Connection));
	}
}