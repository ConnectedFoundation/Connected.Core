using Connected.Annotations;
using Connected.Services;
using Connected.Storage.Sql.Transactions;

namespace Connected.Storage.Sql;

[Priority(0)]
internal sealed class OperationProvider(ICancellationContext cancel)
		: StorageOperationProvider
{
	private ICancellationContext Cancel { get; } = cancel;

	protected override async Task<IStorageOperation?> OnInvoke<TEntity>(TEntity entity)
	{
		var command = new AggregatedCommandBuilder<TEntity>();

		return await command.Build(entity, Cancel.CancellationToken);
	}
}