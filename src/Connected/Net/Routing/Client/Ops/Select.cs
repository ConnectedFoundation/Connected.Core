using Connected.Configuration;
using Connected.Entities;
using Connected.Net.Routing.Dtos;
using Connected.Reflection;
using Connected.Services;
using Grpc.Net.Client;

namespace Connected.Net.Routing.Client.Ops;
internal sealed class Select(IRouteCache cache, IConfigurationService configuration)
	: ServiceFunction<ISelectRouteDto, IRoute?>
{
	protected override async Task<IRoute?> OnInvoke()
	{
		var result = await cache.AsEntity(f => string.Equals(f.Service, Dto.Service, StringComparison.OrdinalIgnoreCase)
			&& f.Protocol == Dto.Protocol);

		if (result is not null)
			return result;

		var url = configuration.Routing.RoutingServerUrl ?? throw new NullReferenceException(SR.ErrRoutingServerNotSet);
		using var channel = GrpcChannel.ForAddress(url);
		var client = new Grpc.RoutingService.RoutingServiceClient(channel);
		var route = await client.SelectAsync(new Grpc.SelectRouteRequest
		{
			Protocol = (Grpc.RouteProtocol)Dto.Protocol,
			Service = Dto.Service
		});

		if (route is null)
			return null;

		return Serializer.Merge<Route>(route);
	}
}
