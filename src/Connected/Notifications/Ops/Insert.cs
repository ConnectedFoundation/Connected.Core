using Connected.Net.Events;
using Connected.Net.Events.Dtos;
using Connected.Net.Messaging;
using Connected.Net.Messaging.Dtos;
using Connected.Reflection;
using Connected.Services;
using Microsoft.Extensions.Logging;
using System.Reflection;

namespace Connected.Notifications.Ops;

internal sealed class Insert<TService, TDto>(IMiddlewareService middlewares, IEventServer server, EventSubscriptions subscriptions,
	ILogger<Insert<TService, TDto>> logger)
	: ServiceAction<IInsertEventDto<TService, TDto>>
	where TDto : IDto
{
	/*
	 * The event as it was raised: the broadcast runs at commit, when the caller may already have reused its DTO for
	 * another event, so its service, name, payload and target are taken together now.
	 */
	private (TService Service, string Event, TDto Payload, string? Target)? Raised { get; set; }

	protected override async Task OnInvoke()
	{
		Raised = (Dto.Service, Dto.Event, Dto.Dto, Dto.Identity);

		var targetMiddleware = typeof(IEventListener<>);
		var dto = Dto.Dto.GetType().GetImplementedDtos();
		var gt = targetMiddleware.MakeGenericType(dto.Count == 0 ? Dto.Dto.GetType() : dto[0]);
		var caller = new CallerContext { Sender = Dto.Service, Method = Dto.Event };
		var middleware = await middlewares.Query(gt, caller);

		foreach (var m in middleware)
		{
			if (m.GetType().GetMethod(nameof(IEventListener<IDto>.Invoke), BindingFlags.Public | BindingFlags.Instance, [typeof(IOperationState), Dto.Dto.GetType(), typeof(ICallerContext), typeof(string)]) is not MethodInfo method)
				continue;

			try
			{
				var callerContext = Dto.Sender?.GetType().GetProperty(nameof(IServiceOperation<IDto>.Caller))?.GetValue(Dto.Sender) is ICallerContext senderCaller ? senderCaller : caller;

				await method.InvokeAsync(m, Dto.Sender, Dto.Dto, callerContext, Dto.Event);
			}
			catch (Exception ex)
			{
				logger.LogError(ex, "Error invoking event listener '{listener}'", m.GetType().FullName);

				throw;
			}
		}
	}

	protected override async Task OnCommitted()
	{
		await Broadcast();
	}

	private async Task Broadcast()
	{
		if (Raised is not var (service, @event, payload, target) || service is null || @event is null)
			return;

		// A blank target names no one: the event is not sent rather than sent to everyone
		if (target is not null && string.IsNullOrWhiteSpace(target))
		{
			logger.LogWarning("Event '{event}' was raised for a blank identity and was sent to no one.", @event);
			return;
		}

		var url = service.GetType().ResolveServiceUrl();

		if (url is null)
			return;

		var key = $"{url}/{@event}".ToLowerInvariant();
		var clients = subscriptions.Items.TryGetValue(key, out List<IClient>? list) && list is not null ? list : [];

		var ctx = new Dto<ISendContextDto>().Value;

		ctx.Method = "Notify";

		foreach (var client in clients)
		{
			if (target is not null && !string.Equals(client.Identity, target, StringComparison.Ordinal))
				continue;

			var dto = new Dto<ISendEventDto>().Value;

			dto.Client = client;
			dto.Event = @event;
			dto.Service = url;
			dto.Dto = payload;
			dto.Context = ctx;

			await server.Send(dto);
		}
	}
}