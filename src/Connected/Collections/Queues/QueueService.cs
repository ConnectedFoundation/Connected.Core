using Connected.Collections.Queues.Ops;
using Connected.Services;
using System.Collections.Immutable;

namespace Connected.Collections.Queues;

internal sealed class QueueService(IServiceProvider services)
	: Service(services), IQueueService
{
	public async Task Insert<TEntity, TCache, TClient, TDto>(TDto dto, IInsertOptionsDto options)
		where TClient : IQueueClient<TDto>
		where TDto : IDto
		where TEntity : IQueueMessage
		where TCache : IQueueMessageCache<TEntity>
	{
		var op = GetOperation<Insert<TEntity, TCache, TClient, TDto>>();

		op.Options = options;

		await Invoke(op, dto);
	}

	public async Task<IImmutableList<TEntity>> Query<TEntity, TCache>(IQueryDto dto)
		where TEntity : IQueueMessage
		where TCache : IQueueMessageCache<TEntity>
	{
		return await Invoke(GetOperation<Query<TEntity, TCache>>(), dto);
	}

	public async Task Update<TEntity, TCache>(IUpdateDto dto)
		where TEntity : IQueueMessage
		where TCache : IQueueMessageCache<TEntity>
	{
		await Invoke(GetOperation<Update<TEntity, TCache>>(), dto);
	}

	public async Task Delete<TEntity, TCache>(IValueDto<Guid> dto)
		where TEntity : IQueueMessage
		where TCache : IQueueMessageCache<TEntity>
	{
		await Invoke(GetOperation<Delete<TEntity, TCache>>(), dto);
	}
}