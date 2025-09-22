using Connected.Annotations;
using Connected.Services;

namespace Connected.Authorization.Services;
internal sealed class ScopeAuthorizationDto : Dto, IScopeAuthorizationDto
{
	[NonDefault]
	public required ICallerContext Caller { get; set; }

	[NonDefault]
	public required IDto Dto { get; set; }
}
