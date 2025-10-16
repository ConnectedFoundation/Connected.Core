namespace Connected.Net.Events;

internal sealed class SubscriptionClient
{
	public required string Id { get; init; }
	public required string Connection { get; init; }
}