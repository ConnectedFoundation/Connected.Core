using Connected.Services;

namespace Connected.Notifications.Net;

internal sealed class EventNotificationDescriptor : Dto
{
	public required string Service { get; set; }
	public required string Event { get; set; }
	public object? Dto { get; set; }
}
