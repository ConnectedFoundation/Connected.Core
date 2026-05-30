namespace Connected.Services;

public abstract class ServiceFunction<TDto, TReturnValue> : ServiceOperation<TDto>, IFunction<TDto, TReturnValue>
	where TDto : IDto
{
	protected TReturnValue Result { get; private set; } = default!;

	public async Task<TReturnValue> Invoke(TDto dto)
	{
		Dto = dto;

		Result = await OnInvoke();

		return Result;
	}

	protected abstract Task<TReturnValue> OnInvoke();
}
