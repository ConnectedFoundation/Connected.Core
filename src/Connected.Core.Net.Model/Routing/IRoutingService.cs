using Connected.Annotations;
using Connected.Net.Routing.Dtos;
using Connected.Services;

namespace Connected.Net.Routing;

[Service]
public interface IRoutingService
{
	Task<IRoute?> Select(ISelectRouteDto dto);
	Task Update(IPrimaryKeyDto<Guid> dto);
	Task<Guid> Insert(IInsertRouteDto dto);
	Task Delete(IPrimaryKeyDto<Guid> dto);
}
