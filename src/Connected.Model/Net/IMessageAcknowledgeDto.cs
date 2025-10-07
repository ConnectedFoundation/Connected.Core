using Connected.Services;

namespace Connected.Net;

public interface IMessageAcknowledgeDto : IDto
{
	ulong Id { get; set; }
}
