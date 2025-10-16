using Connected.Net.Events.Dtos;
using Connected.Net.Messaging;
using Connected.Services;

namespace Connected.Net.Events.Ops;
internal sealed class Subscribe(EventClients eventClients, EventSubscriptions subscriptions)
	: ServiceAction<ISubscribeEventDto>
{
	protected override async Task OnInvoke()
	{
		var client = eventClients.Select(Dto.Connection);

		if (client is null)
			return;

		var target = $"{Dto.Service}/{Dto.Event}".ToLowerInvariant();

		if (subscriptions.Items.TryGetValue(target, out List<IClient>? clients))
		{
			if (clients is null)
				throw new NullReferenceException("Client list is null.");

			lock (clients)
			{
				if (clients.FirstOrDefault(f => string.Equals(f.Connection, Dto.Connection, StringComparison.OrdinalIgnoreCase)) is not null)
					return;

				clients.Add(client);
			}
		}
		else
		{
			if (!subscriptions.Items.TryAdd(target, [client]))
				throw new InvalidOperationException($"Cannot add subscription ({Dto.Service}/{Dto.Event})");
		}

		await Task.CompletedTask;
	}
}
