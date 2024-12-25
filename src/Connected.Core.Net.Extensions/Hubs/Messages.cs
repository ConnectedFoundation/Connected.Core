﻿using Connected.Collections;
using System.Collections.Immutable;

namespace Connected.Net.Hubs;

internal sealed class Messages<TArgs>
{
	public Messages()
	{
		Items = new();
	}

	private List<IMessage<TArgs>> Items { get; }
	public bool IsEmpty => !Items.Any();
	public ImmutableList<IMessage<TArgs>> All() => Items.ToImmutableList(true);

	public void Scave()
	{
		var items = All().Where(f => f.Expire <= DateTime.UtcNow);

		foreach (var item in items)
			Items.Remove(item);
	}

	public ImmutableList<IMessage<TArgs>> Dequeue()
	{
		var items = All().Where(f => f.NextVisible <= DateTime.UtcNow).ToImmutableList(true);

		if (!items.Any())
			return ImmutableList<IMessage<TArgs>>.Empty;

		foreach (var item in items)
			item.NextVisible = item.NextVisible.AddSeconds(5);

		return items;
	}

	public void Remove(string connectionId)
	{
		var items = All().Where(f => string.Equals(f.Client.Connection, connectionId, StringComparison.OrdinalIgnoreCase));

		foreach (var item in items)
			Items.Remove(item);
	}

	public void Remove(ulong id)
	{
		if (All().FirstOrDefault(f => f.Id == id) is Message<TArgs> message)
			Items.Remove(message);
	}

	public void Remap(string connection)
	{
		foreach (var item in All())
			item.Client.Connection = connection;
	}

	public void Remove(string connection, string key)
	{
		var obsolete = All().Where(f => string.Equals(f.Key, key, StringComparison.OrdinalIgnoreCase)
			&& string.Equals(f.Client.Connection, connection, StringComparison.OrdinalIgnoreCase));

		foreach (var o in obsolete)
			Items.Remove(o);
	}

	public void Add(IMessage<TArgs> message)
	{
		Items.Add(message);
	}
}