using Connected.Entities;
using Connected.Net.Routing;
using Connected.Services;
using Connected.Workers;
using Microsoft.Extensions.DependencyInjection;

namespace Connected.Core.Routing;
internal sealed class RoutePing : ScheduledWorker
{
	public RoutePing()
	{
		Timer = TimeSpan.FromSeconds(10);
	}

	protected override async Task OnInvoke(CancellationToken cancellationToken)
	{
		using var scope = Scope.Create();

		var cache = scope.ServiceProvider.GetRequiredService<IRouteCache>();
		var service = scope.ServiceProvider.GetRequiredService<IRoutingService>();

		foreach (var item in await cache.AsEntities())
			await service.Update(Dto.Factory.CreatePrimaryKey(item.Id));

		await scope.Commit();
	}
}
