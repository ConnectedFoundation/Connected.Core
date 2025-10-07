using Connected.Net.Rest.Dtos;

namespace Connected.Net.Rest;

public interface IRequestArgumentHandler : IMiddleware
{
	Task Invoke(IRequestArgumentDto dto);
}
