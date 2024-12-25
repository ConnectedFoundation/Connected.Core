namespace Connected.Net;

public class MessageAcknowledgeDto : IMessageAcknowledgeDto
{
	public MessageAcknowledgeDto(ulong id)
	{
		Id = id;
	}

	public ulong Id { get; set; }
}