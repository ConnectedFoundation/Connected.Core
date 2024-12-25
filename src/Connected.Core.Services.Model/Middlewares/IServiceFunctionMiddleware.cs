namespace Connected.Services.Middlewares;

public interface IServiceFunctionMiddleware<TDto, TReturnValue> : IServiceOperationMiddleware
	where TDto : IDto
{
	Task<TReturnValue?> Invoke(TDto dto, TReturnValue? result);
}
