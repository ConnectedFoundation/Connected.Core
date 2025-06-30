using Connected.Annotations;
using Connected.Services;

namespace Connected.Authorization.Services;

public sealed class ServiceOperationAuthorizationDto<TDto> : Dto, IServiceOperationAuthorizationDto<TDto>
	where TDto : IDto
{
	[NonDefault]
	public required ICallerContext Caller { get; set; }

	[NonDefault]
	public required TDto Dto { get; set; }
}