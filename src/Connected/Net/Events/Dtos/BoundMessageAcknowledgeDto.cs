using Connected.Net.Dtos;
using System.ComponentModel.DataAnnotations;

namespace Connected.Net.Events.Dtos;
internal sealed class BoundMessageAcknowledgeDto
	: MessageAcknowledgeDto, IBoundMessageAcknowledgeDto
{
	[Required]
	public required string Connection { get; set; }
}
