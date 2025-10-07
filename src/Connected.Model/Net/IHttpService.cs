using System.Net.Http;

namespace Connected.Net;

public interface IHttpService
{
	HttpClient CreateClient();
}
