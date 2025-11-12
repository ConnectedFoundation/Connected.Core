using Connected.Annotations;
using Connected.Authorization.Services;
using Connected.Services;

namespace Connected.Authorization;
internal sealed class ServiceOperationAuthorizationDto<TDto>
	: Dto, IServiceOperationAuthorizationDto<TDto>
	where TDto : IDto
{
	[SkipValidation]
	public required ICallerContext Caller { get; set; }

	[SkipValidation]
	public required TDto Dto { get; set; }
}
