using Connected.Entities.Concurrency;

namespace Connected.Net.Routing;
internal sealed record Route : ConcurrentEntity<Guid>, IRoute
{
	public required string Service { get; init; }
	public RouteProtocol Protocol { get; init; }
	public required string Url { get; init; }
}
