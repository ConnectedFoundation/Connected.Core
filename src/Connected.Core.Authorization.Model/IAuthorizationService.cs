namespace Connected.Authorization;

public interface IAuthorizationService
{
	Task<IAuthorizationResult> Authorize(IAuthorizationDto dto);
}
