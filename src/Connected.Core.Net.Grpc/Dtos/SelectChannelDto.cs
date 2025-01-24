using Connected.Annotations;
using Connected.Net.Grpc.Dtos;
using Connected.Services;

namespace Connected.Core.Net.Grpc.Dtos;
internal sealed class SelectChannelDto : Dto, ISelectChannelDto
{
	[NonDefault, SkipValidation]
	public required Type Service { get; set; }
}
