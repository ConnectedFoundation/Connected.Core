using Connected.Net.Dtos;
using Connected.Net.Events;
using Connected.Services;
using Connected.Workers;
using Microsoft.AspNetCore.SignalR;

namespace Connected.Net.Messaging;

internal sealed class EventWorker
	: ScheduledWorker
{
	public EventWorker(IHubContext<EventHub> hub, EventMessages messages, EventClients clients)
	{
		Timer = TimeSpan.FromMilliseconds(500);
		Hub = hub;
		Messages = messages;
		Clients = clients;
	}

	private IHubContext<EventHub> Hub { get; }
	private EventMessages Messages { get; }
	private EventClients Clients { get; }

	protected override async Task OnInvoke(CancellationToken cancellationToken)
	{
		await Send(cancellationToken);
		/*
		 * Clean up every 15 seconds 
		 */
		if (Count % 30 == 0)
			await Clean();
	}

	private async Task Send(CancellationToken cancellationToken)
	{
		foreach (var item in Messages.Dequeue())
		{
			var dto = new Dto<IMessageAcknowledgeDto>().Value;

			dto.Id = item.Id;

			await Hub.Clients.Client(item.Client.Connection).SendCoreAsync("Notify", [dto, item.Service, item.Event, item.Dto], cancellationToken);
		}
	}

	private async Task Clean()
	{
		Messages.Clean();
		Clients.Clean();

		await Task.CompletedTask;
	}
}
