using Connected.Services;

namespace Connected.Net.Routing.Server.Ops;
internal sealed class Delete(IRouteCache cache) : ServiceAction<IPrimaryKeyDto<Guid>>
{
	protected override async Task OnInvoke()
	{
		await cache.Remove(Dto.Id);
	}
}
