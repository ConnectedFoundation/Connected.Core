using Connected.Configuration;
using Connected.Services;
using Grpc.Net.Client;

namespace Connected.Core.Routing.Ops;
internal sealed class Delete(IRouteCache cache, IConfigurationService configuration) : ServiceAction<IPrimaryKeyDto<Guid>>
{
	protected override async Task OnInvoke()
	{
		var url = configuration.Routing.RoutingServerUrl ?? throw new NullReferenceException(SR.ErrRoutingServerNotSet);
		var existing = SetState(await cache.Get(Dto.Id));

		if (existing is null)
			return;

		using var channel = GrpcChannel.ForAddress(url);
		var client = new Net.Grpc.RoutingService.RoutingServiceClient(channel);

		await client.DeleteAsync(new Net.Grpc.IdRequest { Id = Dto.Id.ToString() });
		await cache.Remove(Dto.Id);
	}
}
