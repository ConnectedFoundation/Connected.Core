using Connected.Annotations;
using Connected.Collections.Queues.Dtos;
using Connected.Services;

namespace Connected.Collections.Queues.ValueProviders;

[Middleware<IQueueService>(nameof(IQueueService.Insert))]
internal sealed class InsertValueProvider : DtoValuesProvider<InsertOptionsDto>
{
	protected override Task OnInvoke()
	{
		if (Dto is null)
			return Task.CompletedTask;

		if (Dto.NextVisible is null)
			Dto.NextVisible = DateTimeOffset.UtcNow;

		return Task.CompletedTask;
	}
}