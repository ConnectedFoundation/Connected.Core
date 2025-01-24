using Connected.Configuration;
using Connected.Services;
using Grpc.Net.Client;

namespace Connected.Core.Routing.Ops;
internal sealed class Update(IConfigurationService configuration)
	: ServiceAction<IPrimaryKeyDto<Guid>>
{
	protected override async Task OnInvoke()
	{
		var url = configuration.Routing.RoutingServerUrl ?? throw new NullReferenceException(SR.ErrRoutingServerNotSet);
		using var channel = GrpcChannel.ForAddress(url);
		var client = new Net.Grpc.RoutingService.RoutingServiceClient(channel);
		await client.UpdateAsync(new Net.Grpc.IdRequest
		{
			Id = Dto.Id.ToString()
		});
	}
}
