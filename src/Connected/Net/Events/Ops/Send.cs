using Connected.Net.Dtos;
using Connected.Net.Events.Dtos;
using Connected.Net.Messaging;
using Connected.Services;
using Microsoft.AspNetCore.SignalR;

namespace Connected.Net.Events.Ops;
internal sealed class Send(EventSubscriptions subscriptions, EventMessages messages, IHubContext<EventHub> hub)
	: ServiceAction<ISendEventDto>
{
	protected override async Task OnInvoke()
	{
		if (!subscriptions.Items.TryGetValue(Dto.Key(), out List<IClient>? clients) || clients is null)
			return;

		var message = new EventMessage(Dto.Client, Dto.Dto)
		{
			Event = Dto.Event,
			Service = Dto.Service
		};

		messages.Add(Dto.Client.Connection, message);

		var ack = new Dto<IMessageAcknowledgeDto>().Value;

		ack.Id = message.Id;

		await hub.Clients.Client(Dto.Client.Connection).SendCoreAsync(Dto.Context.Method, [ack, Dto.Service, Dto.Event, Dto.Dto]);
	}
}
