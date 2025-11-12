using Connected.Services;

namespace Connected.Notifications;

public static class NotificationsExtensions
{
	public static Task Inserted<TService, TPrimaryKey>(this IEventService events, IOperationState sender, TService service, TPrimaryKey id)
	{
		return Insert(events, sender, service, id, ServiceEvents.Inserted);
	}

	public static Task Updated<TService, TPrimaryKey>(this IEventService events, IOperationState sender, TService service, TPrimaryKey id)
	{
		return Insert(events, sender, service, id, ServiceEvents.Updated);
	}

	public static Task Deleted<TService, TPrimaryKey>(this IEventService events, IOperationState sender, TService service, TPrimaryKey id)
	{
		return Insert(events, sender, service, id, ServiceEvents.Deleted);
	}

	private static async Task Insert<TService, TPrimaryKey>(this IEventService events, IOperationState sender, TService service, TPrimaryKey id, string @event)
	{
		var dto = Dto.Factory.Create<IInsertEventDto<TService, IPrimaryKeyDto<TPrimaryKey>>>();

		dto.Dto = dto.CreatePrimaryKey(id);
		dto.Event = @event;
		dto.Sender = sender;
		dto.Service = service;

		await events.Insert(dto);
	}
}