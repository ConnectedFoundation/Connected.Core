using Connected.Net.Routing.Server;
using Connected.Services;

namespace Connected.Net.Routing.Server.Ops;
internal sealed class Update(IRouteCache cache)
	: ServiceAction<IPrimaryKeyDto<Guid>>
{
	protected override async Task OnInvoke()
	{
		var entity = await cache.Get(Dto.Id);

		if (entity is null)
			return;

		cache.Set(entity.Id, entity, RoutingUtils.CacheDuration, false);
	}
}
