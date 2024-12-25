using Connected.Collections;
using System.Collections.Concurrent;

namespace Connected.Net.Hubs;

internal sealed class ClientMessages<TDto> : IClientMessages<TDto>
{
	public ClientMessages()
	{
		Clients = new(StringComparer.OrdinalIgnoreCase);
	}

	private ConcurrentDictionary<string, Messages<TDto>> Clients { get; }

	public void Clean()
	{
		foreach (var client in Clients)
		{
			client.Value.Scave();

			if (client.Value.IsEmpty)
				Clients.TryRemove(client.Key, out _);
		}
	}

	public ImmutableList<IMessage<TDto>> Dequeue()
	{
		var result = new List<IMessage<TDto>>();

		foreach (var client in Clients)
		{
			var items = client.Value.Dequeue();

			if (!items.IsEmpty)
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
		if (!Clients.TryGetValue(connection, out Messages<TDto>? items))
			return;

		items.Remove(dto.Id);

		if (items.IsEmpty)
			Clients.TryRemove(connection, out _);
	}

	public void Remove(string connection, string key)
	{
		if (string.IsNullOrEmpty(connection))
			return;

		if (!Clients.TryGetValue(connection, out Messages<TDto>? items))
			return;

		items.Remove(connection, key);

		if (items.IsEmpty)
			Clients.Remove(connection, out _);
	}

	public void Add(string client, IMessage<TDto> message)
	{
		if (!Clients.TryGetValue(client, out Messages<TDto>? items))
		{
			items = new Messages<TDto>();

			if (!Clients.TryAdd(client, items))
				Clients.TryGetValue(client, out items);
		}

		if (items is null)
			throw new NullReferenceException("Could not add client message");

		items.Add(message);
	}
}
