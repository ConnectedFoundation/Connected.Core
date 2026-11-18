using System.Collections.Concurrent;
using System.Collections.Immutable;

namespace Connected.Net.Messaging;

public abstract class Clients
	: IClients
{
	private ConcurrentDictionary<Guid, IClient> Items { get; set; } = [];

	/*
	 * A client and its registry entry change under the client's own lock, so a reconnect, a lost connection and the
	 * cleanup can't undo each other.
	 */
	public void AddOrUpdate(IClient client)
	{
		while (true)
		{
			if (!Items.TryGetValue(client.Id, out IClient? existing))
			{
				if (Items.TryAdd(client.Id, client))
					return;

				continue;
			}

			lock (existing)
			{
				// Removed or replaced meanwhile: decide again on the entry that is there now
				if (!Items.TryGetValue(client.Id, out IClient? current) || !ReferenceEquals(current, existing))
					continue;

				/*
				 * The same client reconnecting as someone else - signed out and in again - is a new client. What the
				 * earlier identity subscribed to and was sent stays with its own connection and lapses with it.
				 */
				if (!string.Equals(existing.Identity, client.Identity, StringComparison.Ordinal))
				{
					Items[client.Id] = client;
					return;
				}

				existing.Connection = client.Connection;
				existing.RetentionDeadline = DateTime.MinValue;
				return;
			}
		}
	}

	public void Clean()
	{
		foreach (var item in Items)
		{
			lock (item.Value)
			{
				// Checked again under the lock: a client that has just reconnected is kept
				if (item.Value.RetentionDeadline != DateTime.MinValue && item.Value.RetentionDeadline <= DateTime.UtcNow)
					Items.TryRemove(item);
			}
		}
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