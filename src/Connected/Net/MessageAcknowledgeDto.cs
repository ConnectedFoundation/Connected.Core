using Connected.Net.Dtos;
using Connected.Services;

namespace Connected.Net;

internal class MessageAcknowledgeDto
	: Dto, IMessageAcknowledgeDto
{
	public ulong Id { get; set; }
}