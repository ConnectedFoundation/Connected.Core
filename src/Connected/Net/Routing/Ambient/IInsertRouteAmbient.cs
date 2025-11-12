using Connected.Net.Routing.Dtos;
using Connected.Services;

namespace Connected.Net.Routing.Ambient;
internal interface IInsertRouteAmbient : IAmbientProvider<IInsertRouteDto>
{
	bool IsRemote { get; set; }
}
