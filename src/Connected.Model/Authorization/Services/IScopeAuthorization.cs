namespace Connected.Authorization.Services;
public interface IScopeAuthorization : IAuthorization
{
	Task<AuthorizationResult> Invoke(IScopeAuthorizationDto dto);
}
