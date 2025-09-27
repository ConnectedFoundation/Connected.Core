namespace Connected.Authorization;

public abstract class AuthorizationMiddleware : Middleware, IAuthorization
{
	public const string NullAuthorizationEntity = "null";
	public const string NullAuthorizationEntityId = "null";
	public bool IsSealed { get; protected set; }

	public abstract string Entity { get; }

	public abstract string EntityId { get; }
}