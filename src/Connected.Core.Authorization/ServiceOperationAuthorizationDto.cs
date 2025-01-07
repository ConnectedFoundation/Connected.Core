using Connected.Annotations;
using Connected.Authorization.Services;
using Connected.Services;

namespace Connected.Authorization;
internal sealed class ServiceOperationAuthorizationDto<TDto> : Dto, IServiceOperationAuthorizationDto<TDto>
	where TDto : IDto
{
	[SkipValidation]
	public ICallerContext? Caller { get; set; }

	[SkipValidation]
	public IDto? Dto { get; set; }
}
