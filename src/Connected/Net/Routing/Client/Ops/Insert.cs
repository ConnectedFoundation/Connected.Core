using Connected.Configuration;
using Connected.Entities;
using Connected.Net.Grpc;
using Connected.Net.Routing.Dtos;
using Connected.Services;
using Grpc.Net.Client;

namespace Connected.Net.Routing.Client.Ops;
internal sealed class Insert(IRouteCache cache, IConfigurationService configuration)
	: ServiceFunction<IInsertRouteDto, Guid>
{
	protected override async Task<Guid> OnInvoke()
	{
		var url = configuration.Routing.RoutingServerUrl ?? throw new NullReferenceException(SR.ErrRoutingServerNotSet);

		using var channel = GrpcChannel.ForAddress(url);
		var client = new Grpc.RoutingService.RoutingServiceClient(channel);
		var id = await client.InsertAsync(new InsertRouteRequest
		{
			Protocol = Dto.Protocol == RouteProtocol.Grpc ? Grpc.RouteProtocol.Grpc : Grpc.RouteProtocol.Http,
			Service = Dto.Service,
			Url = Dto.Url
		});

		var entity = Dto.AsEntity<Route>(State.Update, new
		{
			Id = Guid.Parse(id.Id)
		});

		cache.Set(entity.Id, entity, TimeSpan.Zero);

		return await Task.FromResult(entity.Id);
	}
}
