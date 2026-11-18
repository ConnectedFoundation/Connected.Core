using Connected.Authentication;
using Connected.Net.Dtos;
using Connected.Net.Events.Dtos;
using Connected.Net.Messaging;
using Connected.Services;
using Microsoft.AspNetCore.Http;

namespace Connected.Net.Events;

internal sealed class EventHub(IServiceProvider services, EventClients clients, IAuthenticationService authentication, IHttpContextAccessor http, IEventServer server)
	: ServerHub(services, clients)
{
	public override async Task OnDisconnectedAsync(Exception? ex)
	{
		/*
		 * The client starts its retention and the connection leaves its subscriptions whoever it was signed in as, so
		 * the cleanup runs as the system and never depends on authenticating the connection that is gone.
		 */
		await base.OnDisconnectedAsync(ex);

		try
		{
			await authentication.WithSystemIdentity();

			var dto = new Dto<IUnsubscribeEventDto>().Value;

			dto.Connection = Context.ConnectionId;

			await server.Unsubscribe(dto);
			await Commit();
		}
		catch
		{
			await Rollback();

			throw;
		}
	}

	public async Task Subscribe(List<EventSubscription> subscriptions)
	{
		try
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

			await Commit();
		}
		catch
		{
			await Rollback();

			throw;
		}
	}

	public async Task Unsubscribe(List<EventSubscription> subscriptions)
	{
		try
		{
			await HubAuthentication.Authenticate(Context, clients);
			await authentication.WithRequestIdentity(http);

			foreach (var subscription in subscriptions)
			{
				// An entry without a service or an event names nothing; it never stands for all of them
				if (string.IsNullOrWhiteSpace(subscription.Service) || string.IsNullOrWhiteSpace(subscription.Event))
					continue;

				var dto = new Dto<IUnsubscribeEventDto>().Value;

				dto.Connection = Context.ConnectionId;
				dto.Service = subscription.Service;
				dto.Event = subscription.Event;

				await server.Unsubscribe(dto);
			}

			await Commit();
		}
		catch
		{
			await Rollback();

			throw;
		}
	}

	public async Task Acknowledge(MessageAcknowledgeDto dto)
	{
		try
		{
			await HubAuthentication.Authenticate(Context, clients);
			await authentication.WithRequestIdentity(http);

			var boundDto = new Dto<IBoundMessageAcknowledgeDto>().Value;

			boundDto.Connection = Context.ConnectionId;
			boundDto.Id = dto.Id;

			await server.Acknowledge(boundDto);
			await Commit();
		}
		catch
		{
			await Rollback();

			throw;
		}
	}
}