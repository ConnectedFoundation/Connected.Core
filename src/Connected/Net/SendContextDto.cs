using Connected.Net.Messaging;
using Connected.Services;

namespace Connected.Net;
internal sealed class SendContextDto : Dto, ISendContextDto
{
	public required string Method { get; set; }
	public SendFilterFlags Filter { get; set; }
	public string? Connection { get; set; }
	public long User { get; set; }
}
