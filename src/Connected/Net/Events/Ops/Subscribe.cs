using Connected.Net.Events.Dtos;
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

		var clients = subscriptions.Items.GetOrAdd(target, _ => []);

		lock (clients)
		{
			if (clients.FirstOrDefault(f => string.Equals(f.Connection, Dto.Connection, StringComparison.OrdinalIgnoreCase)) is not null)
				return;

			clients.Add(client);
		}

		await Task.CompletedTask;
	}
}
