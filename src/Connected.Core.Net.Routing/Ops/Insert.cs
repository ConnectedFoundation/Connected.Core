using Connected.Entities;
using Connected.Net.Routing.Ambient;
using Connected.Net.Routing.Dtos;
using Connected.Services;

namespace Connected.Net.Routing.Ops;
internal sealed class Insert(IRouteCache cache, IInsertRouteAmbient ambient)
	: ServiceFunction<IInsertRouteDto, Guid>
{
	protected override async Task<Guid> OnInvoke()
	{
		var entity = Dto.AsEntity<Route>(State.Update, new
		{
			Id = Guid.NewGuid()
		});

		cache.Set(entity.Id, entity, ambient.IsRemote ? RoutingUtils.CacheDuration : TimeSpan.Zero, false);

		return await Task.FromResult(entity.Id);
	}
}
