namespace Connected.Services.Validation;

public interface IValidator<TDto> : IMiddleware where TDto : IDto
{
	Task Invoke(ICallerContext caller, TDto dto);
}
