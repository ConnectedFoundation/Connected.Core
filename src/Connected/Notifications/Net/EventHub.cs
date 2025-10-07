using Connected.Net.Messaging;

namespace Connected.Notifications.Net;

internal sealed class EventHub(EventServer server) : ServerHub<EventNotificationDescriptor>(server)
{
	public override async Task OnDisconnectedAsync(Exception? ex)
	{
		await base.OnDisconnectedAsync(ex);

		((EventServer)Server).Unsubscribe(Context.ConnectionId);
	}

	public void Subscribe(List<EventSubscription> subscriptions)
	{
		((EventServer)Server).Subscribe(Context.ConnectionId, subscriptions);
	}

	public void Unsubscribe(List<EventSubscription> subscriptions)
	{
		((EventServer)Server).Unsubscribe(Context.ConnectionId, subscriptions);
	}
}