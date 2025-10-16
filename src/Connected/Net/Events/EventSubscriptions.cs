using Connected.Net.Messaging;
using System.Collections.Concurrent;

namespace Connected.Net.Events;
internal sealed class EventSubscriptions
{
	public ConcurrentDictionary<string, List<IClient>> Items { get; } = [];
}
