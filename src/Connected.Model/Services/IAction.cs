namespace Connected.Services;

public interface IAction<TDto> : IServiceOperation<TDto>
	where TDto : IDto
{
	Task Invoke(TDto dto);
}
