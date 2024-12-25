using Connected.Services;

namespace Connected.Notifications;

public interface IEventListener<TDto> : IMiddleware
	where TDto : IDto
{
	Task Invoke(IOperationState sender, TDto dto);
}
