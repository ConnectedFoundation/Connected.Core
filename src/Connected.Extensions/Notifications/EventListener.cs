using Connected.Services;

namespace Connected.Notifications;

public abstract class EventListener<TDto> : Middleware, IEventListener<TDto>
	where TDto : IDto
{
	protected TDto Dto { get; private set; } = default!;
	protected IOperationState Sender { get; private set; } = default!;
	protected ICallerContext Caller { get; private set; } = default!;
	protected string EventName { get; private set; } = default!;

	public async Task Invoke(IOperationState sender, TDto dto, ICallerContext caller, string eventName)
	{
		Sender = sender;
		Dto = dto;
		Caller = caller;
		EventName = eventName;

		await OnInvoke();
	}

	protected virtual async Task OnInvoke()
	{
		await Task.CompletedTask;
	}
}
