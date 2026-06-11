using Connected.Services;

namespace Connected.Net.Dtos;

internal class MessageAcknowledgeDto
	: Dto, IMessageAcknowledgeDto
{
	public ulong Id { get; set; }
}