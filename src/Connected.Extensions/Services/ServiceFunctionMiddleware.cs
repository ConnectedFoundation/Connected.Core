using Connected.Services.Middlewares;

namespace Connected.Services;

public abstract class ServiceFunctionMiddleware<TDto, TReturnValue> : ServiceOperationMiddleware, IServiceFunctionMiddleware<TDto, TReturnValue>
	where TDto : IDto
{
	protected IFunction<TDto, TReturnValue> Operation { get; private set; } = default!;
	protected TReturnValue? Result { get; set; }
	public async Task<TReturnValue?> Invoke(IFunction<TDto, TReturnValue> operation, TReturnValue? result)
	{
		Operation = operation;
		Result = result;

		await OnInvoke();

		return result;
	}

	protected virtual async Task OnInvoke()
	{
		await Task.CompletedTask;
	}
}