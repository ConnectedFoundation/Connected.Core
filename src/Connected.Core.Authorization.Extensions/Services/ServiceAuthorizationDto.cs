using Connected.Services;

namespace Connected.Authorization.Services;

public class ServiceAuthorizationDto : Dto
{
	public ICallerContext? Caller { get; set; }
	public IDto? Dto { get; set; }
}