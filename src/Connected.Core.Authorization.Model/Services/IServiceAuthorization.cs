namespace Connected.Authorization.Services;

public interface IServiceAuthorization : IAuthorization
{
	Task Invoke(IServiceAuthorizationDto dto);
}
