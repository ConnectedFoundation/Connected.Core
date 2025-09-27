namespace Connected.Authorization;

public enum AuthorizationResult
{
	Skip = 0,
	Pass = 1,
	Fail = 2
}

public interface IAuthorization : IMiddleware
{
	bool IsSealed { get; }

	string Entity { get; }
	string EntityId { get; }
}