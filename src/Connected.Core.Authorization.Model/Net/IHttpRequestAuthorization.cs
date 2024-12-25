namespace Connected.Authorization.Net;

public interface IHttpRequestAuthorization : IAuthorization
{
	Task Invoke();
}