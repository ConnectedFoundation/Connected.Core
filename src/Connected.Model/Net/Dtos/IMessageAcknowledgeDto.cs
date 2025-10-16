using Connected.Services;

namespace Connected.Net.Dtos;

public interface IMessageAcknowledgeDto : IDto
{
	ulong Id { get; set; }
}
