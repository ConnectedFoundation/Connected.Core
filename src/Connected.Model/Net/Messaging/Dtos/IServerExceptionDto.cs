using Connected.Services;

namespace Connected.Net.Messaging.Dtos;

public interface IServerExceptionDto
	: IDto
{
	string? Message { get; set; }
}
