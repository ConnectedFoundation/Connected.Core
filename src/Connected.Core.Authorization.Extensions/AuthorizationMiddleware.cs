namespace Connected.Authorization;

public abstract class AuthorizationMiddleware : Middleware, IAuthorization
{
	public bool IsSealed { get; protected set; }
}