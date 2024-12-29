using Connected.Services;

namespace Connected.Net.Messaging;

public interface IServerExceptionDto : IDto
{
	string? Message { get; set; }
}
