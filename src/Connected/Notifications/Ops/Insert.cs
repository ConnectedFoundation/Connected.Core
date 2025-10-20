using Connected.Net.Events;
using Connected.Net.Events.Dtos;
using Connected.Net.Messaging;
using Connected.Net.Messaging.Dtos;
using Connected.Reflection;
using Connected.Services;
using System.Reflection;

namespace Connected.Notifications.Ops;

internal sealed class Insert<TService, TDto>(IMiddlewareService middlewares, IEventServer server, EventSubscriptions subscriptions)
	: ServiceAction<IInsertEventDto<TService, TDto>>
	where TDto : IDto
{
	protected override async Task OnInvoke()
	{
		var targetMiddleware = typeof(IEventListener<>);
		var dto = Dto.Dto.GetType().GetImplementedDtos();
		var gt = targetMiddleware.MakeGenericType(dto.Count == 0 ? Dto.Dto.GetType() : dto[0]);
		var middleware = await middlewares.Query(gt, new CallerContext { Sender = Dto.Service, Method = Dto.Event });

		foreach (var m in middleware)
		{
			if (m.GetType().GetMethod(nameof(IEventListener<IDto>.Invoke), BindingFlags.Public | BindingFlags.Instance, [typeof(IOperationState), Dto.Dto.GetType()]) is not MethodInfo method)
				continue;

			await method.InvokeAsync(m, Dto.Sender, Dto.Dto);
		}
	}

	protected override async Task OnCommitted()
	{
		await Broadcast();
	}

	private async Task Broadcast()
	{
		if (Dto.Service is null || Dto.Event is null)
			return;

		var url = Dto.Service.GetType().ResolveServiceUrl();

		if (url is null)
			return;

		var key = $"{url}/{Dto.Event}".ToLowerInvariant();
		var clients = subscriptions.Items.TryGetValue(key, out List<IClient>? list) && list is not null ? list : [];

		var ctx = new Dto<ISendContextDto>().Value;

		ctx.Method = "Notify";

		foreach (var client in clients)
		{
			var dto = new Dto<ISendEventDto>().Value;

			dto.Client = client;
			dto.Event = Dto.Event;
			dto.Service = url;
			dto.Dto = Dto.Dto;
			dto.Context = ctx;

			await server.Send(dto);
		}
	}
}