namespace Connected.Services;

public interface ICalibrator<TDto> : IMiddleware
	where TDto : IDto
{
	Task Invoke(TDto dto);
}