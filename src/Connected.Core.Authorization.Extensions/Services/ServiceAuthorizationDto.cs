using Connected.Annotations;
using Connected.Services;

namespace Connected.Authorization.Services;

public class ServiceAuthorizationDto : Dto, IServiceAuthorizationDto
{
	[NonDefault]
	public required ICallerContext Caller { get; set; }

	[NonDefault]
	public required IDto Dto { get; set; }
}