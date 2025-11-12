using Connected.Services;
using System.ComponentModel.DataAnnotations;

namespace Connected.Net.Events.Dtos;
internal abstract class EventServerDto
	: Dto, IEventServerDto
{
	[Required]
	public required string Connection { get; set; }
}
