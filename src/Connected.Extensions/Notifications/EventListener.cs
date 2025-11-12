using Connected.Services;

namespace Connected.Notifications;

public abstract class EventListener<TDto> : Middleware, IEventListener<TDto>
	where TDto : IDto
{
	protected TDto Dto { get; private set; } = default!;
	protected IOperationState Sender { get; private set; } = default!;

	public async Task Invoke(IOperationState sender, TDto dto)
	{
		Sender = sender;
		Dto = dto;

		await OnInvoke();
	}

	protected virtual async Task OnInvoke()
	{
		await Task.CompletedTask;
	}
}
