using Connected.Net.Events.Dtos;
using Connected.Net.Messaging;
using Connected.Services;

namespace Connected.Net.Events.Ops;
internal sealed class Unsubscribe(EventSubscriptions subscriptions)
	: ServiceAction<IUnsubscribeEventDto>
{
	protected override async Task OnInvoke()
	{
		var key = $"{Dto.Service}/{Dto.Event}".ToLowerInvariant();

		if (!subscriptions.Items.TryGetValue(key, out List<IClient>? clients))
			return;

		lock (clients)
		{
			if (clients.FirstOrDefault(f => string.Equals(f.Connection, Dto.Connection, StringComparison.OrdinalIgnoreCase)) is not IClient client)
				return;

			clients.Remove(client);
		}

		await Task.CompletedTask;
	}
}
