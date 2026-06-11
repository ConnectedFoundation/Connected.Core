using Connected.Net.Messaging.Dtos;
using Connected.Services;

namespace Connected.Net.Dtos;

internal sealed class ServerExceptionDto
	: Dto, IServerExceptionDto
{
	public string? Message { get; set; }
}
