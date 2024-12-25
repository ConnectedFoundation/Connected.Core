namespace Connected.Services.Middlewares;

public interface IServiceActionMiddleware<TDto> : IServiceOperationMiddleware
	where TDto : IDto
{
	Task Invoke(TDto dto);
}