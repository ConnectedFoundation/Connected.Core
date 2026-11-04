using Connected.Services;

namespace Connected.Notifications;

/// <summary>
/// Raises the inserted, updated and deleted events of a service.
/// </summary>
/// <remarks>
/// The id may be <c>null</c> when the change has no key, and the listeners of the event then receive a null id, which they
/// have to check before using it. This works for reference type ids only, such as a string token. A value type id cannot
/// be missing: <c>0</c> is delivered as a real key, and a nullable value type such as <c>long?</c> raises the event for
/// <c>IPrimaryKeyDto&lt;long?&gt;</c>, which the listeners declared on <c>IPrimaryKeyDto&lt;long&gt;</c> do not match.
/// </remarks>
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