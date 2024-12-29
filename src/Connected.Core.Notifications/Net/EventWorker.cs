using Connected.Net.Messaging;
using Microsoft.AspNetCore.SignalR;

namespace Connected.Notifications.Net;

internal sealed class EventWorker(EventServer server, IHubContext<EventHub> hub)
	: ServerCleanupWorker<EventNotificationDescriptor, EventHub>(server, hub)
{
}
