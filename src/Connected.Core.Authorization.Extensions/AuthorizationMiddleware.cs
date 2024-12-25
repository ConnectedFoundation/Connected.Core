namespace Connected.Authorization;

public abstract class AuthorizationMiddleware : MiddlewareComponent, IAuthorization
{
	public bool IsSealed { get; protected set; }
}