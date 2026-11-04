using Connected.Services;

namespace Connected.Notifications;

public static class NotificationsExtensions
{
	public static Task Inserted<TService, TPrimaryKey>(this IEventService events, IOperationState sender, TService service, TPrimaryKey? id)
	{
		return Insert(events, sender, service, id, ServiceEvents.Inserted);
	}

	public static Task Updated<TService, TPrimaryKey>(this IEventService events, IOperationState sender, TService service, TPrimaryKey? id)
	{
		return Insert(events, sender, service, id, ServiceEvents.Updated);
	}

	public static Task Deleted<TService, TPrimaryKey>(this IEventService events, IOperationState sender, TService service, TPrimaryKey? id)
	{
		return Insert(events, sender, service, id, ServiceEvents.Deleted);
	}

	private static async Task Insert<TService, TPrimaryKey>(this IEventService events, IOperationState sender, TService service, TPrimaryKey? id, string @event)
	{
		var primaryKey = new EventIdDto<TPrimaryKey?>
		{
			Id = id
		};

		var dto = DtoFactory.Create<IInsertEventDto<TService, IPrimaryKeyDto<TPrimaryKey?>>>(f =>
		{
			f.Dto = primaryKey;
			f.Event = @event;
			f.Sender = sender;
			f.Service = service;
		});

		await events.Insert(dto);
	}
}