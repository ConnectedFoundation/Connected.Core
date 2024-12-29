using Connected.Services;

namespace Connected.Notifications;

internal sealed class EventService(IServiceProvider services) : Service(services), IEventService
{
	public async Task Insert<TService, TDto>(IInsertEventDto<TService, TDto> dto)
		where TDto : IDto
	{
		await Invoke(GetOperation<Insert<TService, TDto>>(), dto);
	}
}
