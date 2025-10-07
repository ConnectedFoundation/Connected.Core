namespace Connected.Services;

public interface IServiceOperation<TDto> : IOperationState
	where TDto : IDto
{
	TDto Dto { get; }
	ICallerContext Caller { get; }
}
