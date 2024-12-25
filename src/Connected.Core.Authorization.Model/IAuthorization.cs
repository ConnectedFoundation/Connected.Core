namespace Connected.Authorization;

public interface IAuthorization : IMiddleware
{
	bool IsSealed { get; }
}