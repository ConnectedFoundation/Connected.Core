using System.ComponentModel.DataAnnotations;

namespace Connected.Net.Events.Dtos;
internal sealed class SubscribeEventDto
	: EventServerDto, ISubscribeEventDto
{
	[Required]
	public required string Service { get; set; }

	[Required]
	public required string Event { get; set; }
}
