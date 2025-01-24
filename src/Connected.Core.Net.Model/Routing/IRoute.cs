using Connected.Entities;

namespace Connected.Net.Routing;

public enum RouteProtocol
{
	Http = 1,
	Grpc = 2
}
public interface IRoute : IEntity<Guid>
{
	string Service { get; init; }
	RouteProtocol Protocol { get; init; }
	string Url { get; init; }
}
