using Connected.Annotations;
using Connected.Net.Grpc;
using Connected.Net.Routing.Ambient;
using Connected.Net.Routing.Dtos;
using Connected.Services;
using Grpc.Core;
using Microsoft.Extensions.DependencyInjection;

namespace Connected.Net.Routing;

[ServiceProxy<IRoutingService>]
internal sealed class RoutingProxy : Grpc.RoutingService.RoutingServiceBase
{
	public override async Task<VoidResponse> Delete(IdRequest request, ServerCallContext context)
	{
		return await context.Invoke<IRoutingService, IPrimaryKeyDto<Guid>, VoidResponse>(nameof(IRoutingService.Delete), request);
	}
	public override async Task<IdResponse> Insert(InsertRouteRequest request, ServerCallContext context)
	{
		return await context.Invoke<IRoutingService, IInsertRouteDto, IdResponse>(nameof(IRoutingService.Insert), request, async (f) =>
		{
			var ambient = f.ServiceProvider.GetRequiredService<IInsertRouteAmbient>();

			ambient.IsRemote = true;

			await Task.CompletedTask;
		});
	}

	public override async Task<RouteResponse> Select(SelectRouteRequest request, ServerCallContext context)
	{
		return await context.Invoke<IRoutingService, ISelectRouteDto, RouteResponse>(nameof(IRoutingService.Select), request);
	}

	public override async Task<VoidResponse> Update(IdRequest request, ServerCallContext context)
	{
		return await context.Invoke<IRoutingService, IPrimaryKeyDto<Guid>, VoidResponse>(nameof(IRoutingService.Update), request);
	}
}
