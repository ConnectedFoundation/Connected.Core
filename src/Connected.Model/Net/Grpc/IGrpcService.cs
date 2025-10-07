using Connected.Annotations;
using Connected.Net.Grpc.Dtos;
using Grpc.Net.Client;

namespace Connected.Net.Grpc;

[Service]
public interface IGrpcService
{
	Task<GrpcChannel?> SelectChannel(ISelectChannelDto dto);
}
