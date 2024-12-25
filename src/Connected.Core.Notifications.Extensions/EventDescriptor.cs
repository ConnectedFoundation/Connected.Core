using Connected.Services;

namespace Connected.Notifications;

public sealed class EventDescriptor : IEventDescriptor
{
	public IOperationState? Sender { get; init; }
	public Type? Service { get; init; }
	public string? Event { get; init; }
	public EventOrigin Origin { get; init; }
	public object? Dto { get; init; }
}
