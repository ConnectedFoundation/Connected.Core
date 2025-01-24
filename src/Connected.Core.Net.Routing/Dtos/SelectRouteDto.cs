using Connected.Services;
using System.ComponentModel.DataAnnotations;

namespace Connected.Net.Routing.Dtos;
internal sealed class SelectRouteDto : Dto, ISelectRouteDto
{
	public RouteProtocol Protocol { get; set; } = RouteProtocol.Http;

	[Required, MaxLength(1024)]
	public required string Service { get; set; }
}
