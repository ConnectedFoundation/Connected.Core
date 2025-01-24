using Connected.Services;

namespace Connected.Net.Routing.Dtos;
public interface IInsertRouteDto : IDto
{
	RouteProtocol Protocol { get; set; }
	string Service { get; set; }
	string Url { get; set; }
}
