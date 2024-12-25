using Connected.Services;

namespace Connected.Authorization.Services;

public sealed class ServiceOperationAuthorizationDto<TDto> : Dto
	where TDto : IDto
{
	public ICallerContext? Caller { get; set; }
	public IDto? Dto { get; set; }
}