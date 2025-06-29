namespace Connected.Authorization;

public abstract class AuthorizationMiddleware : Middleware, IAuthorization
{
	public bool IsSealed { get; protected set; }

	public virtual string? Type { get; }

	public virtual string? PrimaryKey { get; }
}