using Connected.Collections.Queues.Ops;
using Connected.Services;
using System.Collections.Immutable;

namespace Connected.Collections.Queues;

internal sealed class QueueService(IServiceProvider services)
	: Service(services), IQueueService
{
	public async Task Insert<TClient, TDto>(TDto dto, IInsertOptionsDto options)
		where TClient : IQueueClient<TDto>
		where TDto : IDto
	{
		var op = GetOperation<Insert<TClient, TDto>>();

		op.Options = options;

		await Invoke(op, dto);
	}

	public async Task<IImmutableList<IQueueMessage>> Query(IQueryDto dto)
	{
		return await Invoke(GetOperation<Query>(), dto);
	}

	public async Task Update(IUpdateDto dto)
	{
		await Invoke(GetOperation<Update>(), dto);
	}

	public async Task Delete(IValueDto<Guid> dto)
	{
		await Invoke(GetOperation<Delete>(), dto);
	}
}