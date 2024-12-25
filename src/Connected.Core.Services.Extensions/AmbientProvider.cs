namespace Connected.Services;

public abstract class AmbientProvider<TDto> : MiddlewareComponent, IAmbientProvider<TDto>
	where TDto : IDto
{
	protected TDto? Dto { get; private set; }

	public async Task Invoke(TDto dto)
	{
		Dto = dto;

		await OnInvoke();
	}

	protected virtual Task OnInvoke()
	{
		return Task.CompletedTask;
	}
}