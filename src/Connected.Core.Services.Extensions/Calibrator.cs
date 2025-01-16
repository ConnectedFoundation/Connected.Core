namespace Connected.Services;

public abstract class Calibrator<TDto> : Connected.Middleware, ICalibrator<TDto>
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