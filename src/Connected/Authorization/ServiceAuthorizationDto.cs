using Connected.Annotations;
using Connected.Authorization.Services;
using Connected.Services;

namespace Connected.Authorization;
internal sealed class ServiceAuthorizationDto : Dto, IServiceAuthorizationDto
{
	[SkipValidation]
	public required ICallerContext Caller { get; set; }

	[SkipValidation]
	public required IDto Dto { get; set; }
}
