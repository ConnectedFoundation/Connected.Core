namespace Connected.Services;

public interface IServiceOperation<TDto>
	where TDto : IDto
{
	TDto Dto { get; }
	ICallerContext Caller { get; }
}
