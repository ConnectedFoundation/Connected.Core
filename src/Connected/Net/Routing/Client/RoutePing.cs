using Connected.Authentication;
using Connected.Entities;
using Connected.Services;
using Connected.Workers;
using Microsoft.Extensions.DependencyInjection;

namespace Connected.Net.Routing.Client;
internal sealed class RoutePing : ScheduledWorker
{
	public RoutePing()
	{
		Timer = TimeSpan.FromSeconds(10);
	}

	protected override async Task OnInvoke(CancellationToken cancellationToken)
	{
		using var scope = await Scope.Create().WithSystemIdentity();

		var cache = scope.ServiceProvider.GetRequiredService<IRouteCache>();
		var service = scope.ServiceProvider.GetRequiredService<IRoutingService>();

		foreach (var item in await cache.AsEntities())
			await service.Update(Dto.Factory.CreatePrimaryKey(item.Id));

		await scope.Commit();
	}
}
