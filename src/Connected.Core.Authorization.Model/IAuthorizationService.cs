using Connected.Annotations;

namespace Connected.Authorization;

[Service]
public interface IAuthorizationService
{
	Task<IAuthorizationResult> Authorize(IAuthorizationDto dto);
}
