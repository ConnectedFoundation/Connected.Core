using Connected.Net.Messaging;
using Connected.Net.Messaging.Dtos;
using Connected.Services;

namespace Connected.Net.Events.Dtos;

public interface ISendEventDto
	: IDto
{
	ISendContextDto Context { get; set; }
	IClient Client { get; set; }
	IDto Dto { get; set; }

	string Service { get; set; }
	string Event { get; set; }
}
