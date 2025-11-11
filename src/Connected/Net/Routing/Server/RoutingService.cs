using Connected.Annotations;
using Connected.Net.Routing.Dtos;
using Connected.Net.Routing.Server.Ops;
using Connected.Services;

namespace Connected.Net.Routing.Server;

[ServiceRegistration(ServiceRegistrationMode.Manual)]
internal sealed class RoutingService(IServiceProvider services)
	: Service(services), IRoutingService
{
	public async Task Delete(IPrimaryKeyDto<Guid> dto)
	{
		await Invoke(GetOperation<Delete>(), dto);
	}

	public async Task<Guid> Insert(IInsertRouteDto dto)
	{
		return await Invoke(GetOperation<Insert>(), dto);
	}

	public async Task<IRoute?> Select(ISelectRouteDto dto)
	{
		return await Invoke(GetOperation<Select>(), dto);
	}

	public Task Update(IPrimaryKeyDto<Guid> dto)
	{
		return Invoke(GetOperation<Update>(), dto);
	}
}
