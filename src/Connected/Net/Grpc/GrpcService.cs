using Connected.Net.Grpc.Dtos;
using Connected.Net.Grpc.Ops;
using Connected.Services;
using Grpc.Net.Client;

namespace Connected.Net.Grpc;
internal sealed class GrpcService(IServiceProvider services)
	: Service(services), IGrpcService
{
	public async Task<GrpcChannel?> SelectChannel(ISelectChannelDto dto)
	{
		return await Invoke(GetOperation<SelectChannel>(), dto);
	}
}
