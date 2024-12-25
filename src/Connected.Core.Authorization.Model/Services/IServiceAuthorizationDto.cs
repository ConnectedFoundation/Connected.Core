using Connected.Services;

namespace Connected.Authorization.Services;
public interface IServiceAuthorizationDto : IDto
{
	ICallerContext? Caller { get; set; }
	IDto? Dto { get; set; }
}