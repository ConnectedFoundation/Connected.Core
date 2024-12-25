using Connected.Services.Middlewares;

namespace Connected.Services.Middleware;

public abstract class ServiceFunctionMiddleware<TDto, TReturnValue> : ServiceOperationMiddleware, IServiceFunctionMiddleware<TDto, TReturnValue>
	where TDto : IDto
{
	protected TDto Dto { get; private set; } = default!;

	public async Task<TReturnValue?> Invoke(TDto dto, TReturnValue? result)
	{
		Dto = dto;

		return await OnInvoke(result);
	}

	protected async Task<TReturnValue?> OnInvoke(TReturnValue? result)
	{
		await Task.CompletedTask;

		return result;
	}
}