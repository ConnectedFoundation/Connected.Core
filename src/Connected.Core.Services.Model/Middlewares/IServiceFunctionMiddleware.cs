namespace Connected.Services.Middlewares;

public interface IServiceFunctionMiddleware<TDto, TReturnValue> : IServiceOperationMiddleware
	where TDto : IDto
{
	Task<TReturnValue?> Invoke(IFunction<TDto, TReturnValue> operation, TReturnValue? result);
}
