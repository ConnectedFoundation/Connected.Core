using Connected.Entities;

namespace Connected.Net.Routing.Server;
internal sealed record Route : ConcurrentEntity<Guid>, IRoute
{
	public required string Service { get; init; }
	public RouteProtocol Protocol { get; init; }
	public required string Url { get; init; }
}
