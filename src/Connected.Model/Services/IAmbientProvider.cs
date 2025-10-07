namespace Connected.Services;

public interface IAmbientProvider<TDto> : IMiddleware
	where TDto : IDto
{
	Task Invoke(TDto dto);
}
