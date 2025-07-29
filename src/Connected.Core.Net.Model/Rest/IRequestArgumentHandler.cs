using Connected.Net.Dtos;

namespace Connected.Net;

public interface IRequestArgumentHandler : IMiddleware
{
	Task Invoke(IRequestArgumentDto dto);
}
