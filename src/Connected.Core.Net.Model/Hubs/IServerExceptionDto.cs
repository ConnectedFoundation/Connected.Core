using Connected.Services;

namespace Connected.Net.Hubs;

public interface IServerExceptionDto : IDto
{
	string? Message { get; set; }
}
