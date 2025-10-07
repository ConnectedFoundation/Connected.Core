using Connected.Services;

namespace Connected.Net.Routing.Dtos;
public interface ISelectRouteDto : IDto
{
	RouteProtocol Protocol { get; set; }
	string Service { get; set; }
}
