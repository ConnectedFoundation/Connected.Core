using Connected.Net.Messaging;
using Connected.Services;

namespace Connected.Net.Events;
internal sealed class EventMessage(IClient client, IDto dto)
		: Message(client, dto)
{
	public required string Service { get; set; }
	public required string Event { get; set; }
}
