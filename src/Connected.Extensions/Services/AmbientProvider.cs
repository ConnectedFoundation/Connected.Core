namespace Connected.Services;

public abstract class AmbientProvider<TDto> : Connected.Middleware, IAmbientProvider<TDto>
	where TDto : IDto
{
	protected TDto Dto { get; private set; } = default!;

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