using Connected.Net.Routing.Dtos;
using Connected.Services;

namespace Connected.Net.Routing.Ambient;
internal sealed class InsertRouteAmbient : AmbientProvider<IInsertRouteDto>, IInsertRouteAmbient
{
	public bool IsRemote { get; set; }
}
