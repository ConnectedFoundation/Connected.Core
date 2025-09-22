using Connected.Services;

namespace Connected.Authorization.Services;
public interface IScopeAuthorizationDto : IDto
{
	ICallerContext Caller { get; set; }
	IDto Dto { get; set; }
}
