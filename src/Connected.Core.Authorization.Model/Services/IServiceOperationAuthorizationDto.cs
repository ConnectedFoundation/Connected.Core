using Connected.Services;

namespace Connected.Authorization.Services;
public interface IServiceOperationAuthorizationDto<TDto> : IDto
	where TDto : IDto
{
	ICallerContext? Caller { get; set; }
	IDto? Dto { get; set; }
}