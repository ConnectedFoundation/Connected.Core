using Connected.Services;

namespace Connected.Net.Grpc.Dtos;
public interface ISelectChannelDto : IDto
{
	Type Service { get; set; }
}
