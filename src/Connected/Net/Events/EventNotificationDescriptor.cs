using Connected.Services;

namespace Connected.Net.Events;

internal sealed class EventNotificationDescriptor
	: Dto
{
	public required string Service { get; set; }
	public required string Event { get; set; }
	public required IDto Dto { get; set; }
}
