using System.Collections.Concurrent;
using System.Collections.Immutable;

namespace Connected.Net.Messaging;

public abstract class Clients
	: IClients
{
	private ConcurrentDictionary<Guid, IClient> Items { get; set; } = [];

	public void AddOrUpdate(IClient client)
	{
		if (!Items.TryGetValue(client.Id, out IClient? existing))
		{
			Items.TryAdd(client.Id, client);
			return;
		}

		existing.Connection = client.Connection;
		existing.RetentionDeadline = DateTime.MinValue;
	}

	public void Clean()
	{
		var dead = Items.Where(f => f.Value.RetentionDeadline != DateTime.MinValue && f.Value.RetentionDeadline <= DateTime.UtcNow).ToImmutableList();

		if (dead.IsEmpty)
			return;

		foreach (var client in dead)
			Items.TryRemove(client.Key, out _);
	}

	public void Remove(string connectionId)
	{
		if (Select(connectionId) is not IClient client)
			return;

		Items.TryRemove(client.Id, out _);
	}

	public IImmutableList<IClient> Query()
	{
		return Items.Values.ToImmutableList();
	}

	public IClient? Select(Guid id)
	{
		if (Items.TryGetValue(id, out IClient? client))
			return client;

		return null;
	}

	public IClient? Select(string connection)
	{
		return Items.FirstOrDefault(f => string.Equals(f.Value.Connection, connection, StringComparison.OrdinalIgnoreCase)).Value;
	}
}