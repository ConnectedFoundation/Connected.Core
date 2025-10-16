using Connected.Collections;
using Connected.Net.Dtos;
using System.Collections.Concurrent;
using System.Collections.Immutable;

namespace Connected.Net.Messaging;

public abstract class ClientMessages<TMessage>
	: IClientMessages<TMessage>
	where TMessage : IMessage
{
	public ClientMessages()
	{
		Clients = new(StringComparer.OrdinalIgnoreCase);
	}

	private ConcurrentDictionary<string, Messages<TMessage>> Clients { get; }

	public void Clean()
	{
		foreach (var client in Clients)
		{
			client.Value.Scave();

			if (client.Value.IsEmpty)
				Clients.TryRemove(client.Key, out _);
		}
	}

	public IImmutableList<TMessage> Dequeue()
	{
		var result = new List<TMessage>();

		foreach (var client in Clients)
		{
			var items = client.Value.Dequeue();

			if (items.Count != 0)
				result.AddRange(items);
		}

		return result.ToImmutableList(true);
	}

	public void Remove(string connectionId)
	{
		foreach (var client in Clients)
		{
			client.Value.Remove(connectionId);

			if (client.Value.IsEmpty)
				Clients.TryRemove(client.Key, out _);
		}
	}

	public void Remove(string connection, IMessageAcknowledgeDto dto)
	{
		if (!Clients.TryGetValue(connection, out Messages<TMessage>? items))
			return;

		items.Remove(dto.Id);

		if (items.IsEmpty)
			Clients.TryRemove(connection, out _);
	}

	public void Remove(string connection, string key)
	{
		if (string.IsNullOrEmpty(connection))
			return;

		if (!Clients.TryGetValue(connection, out Messages<TMessage>? items))
			return;

		items.Remove(connection, key);

		if (items.IsEmpty)
			Clients.Remove(connection, out _);
	}

	public void Add(string client, TMessage message)
	{
		if (!Clients.TryGetValue(client, out Messages<TMessage>? items))
		{
			items = new Messages<TMessage>();

			if (!Clients.TryAdd(client, items))
				Clients.TryGetValue(client, out items);
		}

		if (items is null)
			throw new NullReferenceException("Could not add client message");

		items.Add(message);
	}
}