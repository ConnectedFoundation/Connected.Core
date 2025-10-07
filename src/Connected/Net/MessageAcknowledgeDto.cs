using Connected.Services;

namespace Connected.Net;

internal sealed class MessageAcknowledgeDto : Dto, IMessageAcknowledgeDto
{
	public ulong Id { get; set; }
}