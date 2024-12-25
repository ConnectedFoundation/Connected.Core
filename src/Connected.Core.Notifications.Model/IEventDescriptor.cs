using Connected.Services;

namespace Connected.Notifications;

public enum EventOrigin
{
	InProcess = 1,
	Remote = 2
}

public interface IEventDescriptor
{
	IOperationState? Sender { get; init; }
	Type? Service { get; init; }
	string? Event { get; init; }
	EventOrigin Origin { get; init; }
	object? Dto { get; init; }
}