using Connected.Authentication;
using Connected.Net.Dtos;
using Connected.Net.Events.Dtos;
using Connected.Net.Messaging;
using Connected.Services;
using Microsoft.AspNetCore.Http;

namespace Connected.Net.Events;

internal sealed class EventHub(IAuthenticationService authentication, IHttpContextAccessor http, IEventServer server, EventClients clients)
	: ServerHub(clients)
{
	public override async Task OnDisconnectedAsync(Exception? ex)
	{
		await HubAuthentication.Authenticate(Context, clients);

		await base.OnDisconnectedAsync(ex);
		await authentication.WithSystemIdentity();

		var dto = new Dto<IUnsubscribeEventDto>().Value;

		dto.Connection = Context.ConnectionId;

		await server.Unsubscribe(dto);
	}

	public async Task Subscribe(List<EventSubscription> subscriptions)
	{
		await HubAuthentication.Authenticate(Context, clients);
		await authentication.WithRequestIdentity(http);

		foreach (var subscription in subscriptions)
		{
			var dto = new Dto<ISubscribeEventDto>().Value;

			dto.Connection = Context.ConnectionId;
			dto.Service = subscription.Service;
			dto.Event = subscription.Event;

			await server.Subscribe(dto);
		}
	}

	public async Task Unsubscribe(List<EventSubscription> subscriptions)
	{
		await HubAuthentication.Authenticate(Context, clients);
		await authentication.WithRequestIdentity(http);

		foreach (var subscription in subscriptions)
		{
			var dto = new Dto<IUnsubscribeEventDto>().Value;

			dto.Connection = Context.ConnectionId;
			dto.Service = subscription.Service;
			dto.Event = subscription.Event;

			await server.Unsubscribe(dto);
		}
	}

	public async Task Acknowledge(MessageAcknowledgeDto dto)
	{
		await HubAuthentication.Authenticate(Context, clients);
		await authentication.WithRequestIdentity(http);

		var boundDto = new Dto<IBoundMessageAcknowledgeDto>().Value;

		boundDto.Connection = Context.ConnectionId;
		boundDto.Id = dto.Id;

		await server.Acknowledge(boundDto);
	}
}