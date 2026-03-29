using Connected.Annotations;
using Connected.Services;
using Connected.Storage.Sql.Transactions;

namespace Connected.Storage.Sql;

[Priority(0)]
internal sealed class OperationProvider(ICancellationContext cancel)
		: StorageOperationProvider
{
	private ICancellationContext Cancel { get; } = cancel;

	protected override async Task<IStorageOperation?> OnInvoke<TEntity>(IStorage<TEntity> storage, TEntity entity, IEnumerable<string>? updatingProperties)
	{
		return await AggregatedCommandBuilder<TEntity>.Build(storage, entity, updatingProperties, Cancel.CancellationToken);
	}
}