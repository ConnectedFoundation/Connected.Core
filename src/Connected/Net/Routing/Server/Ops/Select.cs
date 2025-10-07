using Connected.Entities;
using Connected.Net.Routing.Dtos;
using Connected.Net.Routing.Server;
using Connected.Services;

namespace Connected.Net.Routing.Server.Ops;
internal sealed class Select(IRouteCache cache)
	: ServiceFunction<ISelectRouteDto, IRoute?>
{
	protected override async Task<IRoute?> OnInvoke()
	{
		var result = (await cache.AsEntities(f => string.Equals(f.Service, Dto.Service, StringComparison.OrdinalIgnoreCase)
			&& f.Protocol == Dto.Protocol)).OrderBy(f => f.Sync).FirstOrDefault();

		if (result is not null)
			cache.Set(result.Id, result, RoutingUtils.CacheDuration);

		return result;
	}
}
