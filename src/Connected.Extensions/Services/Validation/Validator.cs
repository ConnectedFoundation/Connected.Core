using Connected.Services;

namespace Connected.Services.Validation;

public abstract class Validator<TDto> : Middleware, IValidator<TDto> where TDto : IDto
{
	protected TDto Dto { get; private set; } = default!;
	protected ICallerContext Caller { get; private set; } = default!;

	public async Task Invoke(ICallerContext caller, TDto dto)
	{
		Caller = caller;
		Dto = dto;

		await OnInvoke();
	}

	protected virtual async Task OnInvoke()
	{
		await Task.CompletedTask;
	}
}