using Connected.Net.Grpc.Dtos;
using Connected.Net.Routing;
using Connected.Net.Routing.Dtos;
using Connected.Services;
using Grpc.Net.Client;

namespace Connected.Net.Grpc.Ops;
internal sealed class SelectChannel(IRoutingService routing)
	: ServiceFunction<ISelectChannelDto, GrpcChannel?>
{
	protected override async Task<GrpcChannel?> OnInvoke()
	{
		if (Dto.Service.FullName is null)
			return null;

		var dto = Dto.Create<ISelectRouteDto>();

		dto.Protocol = Routing.RouteProtocol.Grpc;
		dto.Service = Dto.Service.FullName;

		var route = await routing.Select(dto);

		if (route is null)
			return null;

		return GrpcChannel.ForAddress(route.Url);
	}
}
