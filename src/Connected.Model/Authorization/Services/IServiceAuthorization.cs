namespace Connected.Authorization.Services;

public interface IServiceAuthorization : IAuthorization
{
	Task<AuthorizationResult> Invoke(IServiceAuthorizationDto dto);
}
