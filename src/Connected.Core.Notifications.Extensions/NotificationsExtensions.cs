using Connected.Services;

namespace Connected.Notifications;

public static class NotificationsExtensions
{
	public static Task Inserted<TService, TPrimaryKey>(this IEventService events, IOperationState sender, TService service, TPrimaryKey id)
	{
		return Insert<TService, TPrimaryKey>(events, sender, service, id, ServiceEvents.Inserted);
	}

	public static Task Updated<TService, TPrimaryKey>(this IEventService events, IOperationState sender, TService service, TPrimaryKey id)
	{
		return Insert<TService, TPrimaryKey>(events, sender, service, id, ServiceEvents.Updated);
	}

	public static Task Deleted<TService, TPrimaryKey>(this IEventService events, IOperationState sender, TService service, TPrimaryKey id)
	{
		return Insert<TService, TPrimaryKey>(events, sender, service, id, ServiceEvents.Deleted);
	}

	private static async Task Insert<TService, TPrimaryKey>(this IEventService events, IOperationState sender, TService service, TPrimaryKey id, string @event)
	{
		var dto = Scope.GetDto<IInsertEventDto<TService, PrimaryKeyDto<TPrimaryKey>>>();

		dto.Dto = new PrimaryKeyDto<TPrimaryKey> { Id = id };
		dto.Event = @event;
		dto.Sender = sender;
		dto.Service = service;

		await events.Insert(dto);
	}
}