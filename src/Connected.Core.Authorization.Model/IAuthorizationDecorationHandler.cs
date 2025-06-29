using Connected.Authorization.Dtos;

namespace Connected.Authorization;

public interface IAuthorizationDecorationHandler : IMiddleware
{
	Task<AuthorizationResult> Invoke(IAuthorizationDecorationHandlerDto dto);
}
