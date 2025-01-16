using Connected.Services.Middlewares;

namespace Connected.Services.Middleware;

public abstract class ServiceActionMiddleware<TDto> : Connected.Middleware, IServiceActionMiddleware<TDto>
	where TDto : IDto
{
	protected TDto Dto { get; private set; } = default!;

	public async Task Invoke(TDto dto)
	{
		Dto = dto;

		await OnInvoke();
	}

	protected virtual async Task OnInvoke()
	{
		await Task.CompletedTask;
	}
}