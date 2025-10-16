namespace Connected.Net.Events;

internal sealed class EventSubscription
{
	public required string Service { get; set; }
	public required string Event { get; set; }
}