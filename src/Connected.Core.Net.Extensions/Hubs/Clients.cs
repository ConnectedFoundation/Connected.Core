using System.Collections.Concurrent;
using System;
using System.Linq;
using System.Collections.Immutable;

namespace Connected.Net.Hubs;

internal sealed class Clients : IClients
{
	public Clients()
	{
		Items = new(StringComparer.OrdinalIgnoreCase);
	}

	private ConcurrentDictionary<string, IClient> Items { get; set; }

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
		if (!Items.TryGetValue(connectionId, out IClient? client))
			return;

		client.RetentionDeadline = DateTime.UtcNow.AddMinutes(5);
	}

	public ImmutableList<IClient> Query()
	{
		return Items.Values.ToImmutableList();
	}

	public IClient? Select(string id)
	{
		return Items.FirstOrDefault(f => string.Equals(f.Value.Id, id, StringComparison.OrdinalIgnoreCase)).Value;
	}

	public IClient? SelectByConnection(string connection)
	{
		return Items.FirstOrDefault(f => string.Equals(f.Value.Connection, connection, StringComparison.OrdinalIgnoreCase)).Value;
	}
}