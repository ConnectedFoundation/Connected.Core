using Connected.Net.Messaging;
using Connected.Notifications.Net;
using Connected.Reflection;
using Connected.Services;
using System.Reflection;

namespace Connected.Notifications;

internal sealed class Insert<TService, TDto>(IServiceProvider services, IMiddlewareService middlewares, EventServer server)
	: ServiceAction<IInsertEventDto<TService, TDto>>
	where TDto : IDto
{
	protected override async Task OnInvoke()
	{
		var broadcast = Broadcast();

		var targetMiddleware = typeof(IEventListener<>);
		var gt = targetMiddleware.MakeGenericType(Dto.Dto.GetType());
		var middleware = await middlewares.Query(gt, new CallerContext { Sender = Dto.Service, Method = Dto.Event });

		foreach (var m in middleware)
		{
			if (m.GetType().GetMethod(nameof(IEventListener<IDto>.Invoke), BindingFlags.Public | BindingFlags.Instance, [typeof(IOperationState), Dto.Dto.GetType()]) is not MethodInfo method)
				continue;

			await method.InvokeAsync(m, Dto.Sender, Dto.Dto);
		}

		await broadcast;
	}

	private async Task Broadcast()
	{
		if (Dto.Service is null || Dto.Event is null)
			return;

		var url = Dto.Service.GetType().ResolveServiceUrl();

		if (url is null)
			return;

		var dto = Scope.GetDto<ISendContextDto>();

		dto.Method = "Notify";

		await server.Send(dto, new EventNotificationDescriptor
		{
			Dto = Dto.Dto,
			Event = Dto.Event.ToCamelCase(),
			Service = url.TrimStart('/')
		});
	}
}