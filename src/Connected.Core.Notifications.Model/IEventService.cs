using Connected.Annotations;
using Connected.Services;

namespace Connected.Notifications;

[Service]
public interface IEventService
{
	Task Insert<TService, TDto>(IInsertEventDto<TService, TDto> dto)
		where TDto : IDto;
}
