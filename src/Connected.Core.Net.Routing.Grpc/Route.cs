using Connected.Entities;
using Connected.Net.Routing;

namespace Connected.Core.Routing;
internal sealed record Route : Entity<Guid>, IRoute
{
	public required string Service { get; init; }
	public RouteProtocol Protocol { get; init; }
	public required string Url { get; init; }
}
