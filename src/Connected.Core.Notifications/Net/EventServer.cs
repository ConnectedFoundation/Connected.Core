using Connected.Net.Messaging;
using Microsoft.AspNetCore.SignalR;
using System.Collections.Concurrent;
using System.Collections.Immutable;

namespace Connected.Notifications.Net;

internal sealed class EventServer(IHubContext<EventHub> hub) : Server<EventHub, EventNotificationDescriptor>(hub)
{
	public ConcurrentDictionary<string, List<IClient>> Subscriptions { get; } = new();

	public void Subscribe(string connection, List<EventSubscription> subscriptions)
	{
		if (subscriptions.Count == 0)
			return;

		foreach (var subscription in subscriptions)
			Subscribe(connection, $"{subscription.Service}/{subscription.Event}".ToLowerInvariant());
	}

	private void Subscribe(string connection, string target)
	{
		var client = Clients.SelectByConnection(connection);

		if (client is null)
			return;

		if (Subscriptions.TryGetValue(target, out List<IClient>? clients))
		{
			if (clients is null)
				throw new NullReferenceException("Client list is null.");

			lock (clients)
			{
				if (clients.FirstOrDefault(f => string.Equals(f.Connection, connection, StringComparison.OrdinalIgnoreCase)) is not null)
					return;

				clients.Add(client);
			}
		}
		else
		{
			if (!Subscriptions.TryAdd(target, [client]))
				Subscribe(connection, target);
		}
	}

	public void Unsubscribe(string connection, List<EventSubscription> subscriptions)
	{
		foreach (var subscription in subscriptions)
		{
			var key = $"{subscription.Service}/{subscription.Event}".ToLowerInvariant();

			if (!Subscriptions.TryGetValue(key, out List<IClient>? clients))
				return;

			lock (clients)
			{
				if (clients.FirstOrDefault(f => string.Equals(f.Connection, connection, StringComparison.OrdinalIgnoreCase)) is not IClient client)
					return;

				clients.Remove(client);
			}
		}
	}

	public void Unsubscribe(string connection)
	{
		foreach (var subscription in Subscriptions)
		{
			lock (subscription.Value)
			{
				if (subscription.Value.FirstOrDefault(f => string.Equals(f.Connection, connection, StringComparison.OrdinalIgnoreCase)) is not IClient client)
					return;

				subscription.Value.Remove(client);
			}
		}
	}

	public override async Task Send(ISendContextDto context, EventNotificationDescriptor dto)
	{
		var key = $"{dto.Service}/{dto.Event}".ToLowerInvariant();

		if (!Subscriptions.TryGetValue(key, out List<IClient>? clients) || clients is null)
			return;

		var tasks = new List<Task>();

		lock (clients)
		{
			var candidates = clients.ToImmutableList();

			foreach (var candidate in candidates)
				tasks.Add(Send(context.Method, candidate, dto));
		}

		await Task.WhenAll(tasks);
	}
}
