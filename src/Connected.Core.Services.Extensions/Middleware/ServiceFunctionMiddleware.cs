using Connected.Services.Middlewares;

namespace Connected.Services.Middleware;

public abstract class ServiceFunctionMiddleware<TDto, TReturnValue> : ServiceOperationMiddleware, IServiceFunctionMiddleware<TDto, TReturnValue>
	where TDto : IDto
{
	protected TDto Dto { get; private set; } = default!;
	protected TReturnValue? Result { get; set; }
	public async Task<TReturnValue?> Invoke(TDto dto, TReturnValue? result)
	{
		Dto = dto;
		Result = result;

		await OnInvoke();

		return result;
	}

	protected virtual async Task OnInvoke()
	{
		await Task.CompletedTask;
	}
}