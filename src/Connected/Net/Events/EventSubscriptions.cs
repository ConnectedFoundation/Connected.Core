using Connected.Net.Messaging;
using System.Collections.Concurrent;

namespace Connected.Net.Events;
internal sealed class EventSubscriptions
{
	public ConcurrentDictionary<string, List<IClient>> Items { get; } = [];

	/// <summary>
	/// Removes the connection from every event it subscribed to.
	/// </summary>
	public void Remove(string connection)
	{
		foreach (var subscribed in Items.Values)
		{
			lock (subscribed)
				subscribed.RemoveAll(f => string.Equals(f.Connection, connection, StringComparison.OrdinalIgnoreCase));
		}
	}
}
