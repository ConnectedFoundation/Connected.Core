using Connected.Net.Routing;
using Connected.Net.Routing.Dtos;
using Connected.Services;
using System.ComponentModel.DataAnnotations;

namespace Connected.Core.Routing.Dtos;
internal sealed class InsertRouteDto : Dto, IInsertRouteDto
{
	public RouteProtocol Protocol { get; set; }

	[Required, MaxLength(1024)]
	public required string Service { get; set; }

	[Required, MaxLength(1024)]
	public required string Url { get; set; }
}
