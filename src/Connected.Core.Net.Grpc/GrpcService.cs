using Connected.Core.Net.Grpc.Ops;
using Connected.Net.Grpc;
using Connected.Net.Grpc.Dtos;
using Connected.Services;
using Grpc.Net.Client;

namespace Connected.Core.Net.Grpc;
internal sealed class GrpcService(IServiceProvider services)
	: Service(services), IGrpcService
{
	public async Task<GrpcChannel?> SelectChannel(ISelectChannelDto dto)
	{
		return await Invoke(GetOperation<SelectChannel>(), dto);
	}
}
