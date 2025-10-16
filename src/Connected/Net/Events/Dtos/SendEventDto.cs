using Connected.Annotations;
using Connected.Net.Messaging;
using Connected.Net.Messaging.Dtos;
using Connected.Services;
using System.ComponentModel.DataAnnotations;

namespace Connected.Net.Events.Dtos;
internal sealed class SendEventDto
	: Dto, ISendEventDto
{
	[NonDefault, SkipValidation]
	public required ISendContextDto Context { get; set; }

	[NonDefault, SkipValidation]
	public required IClient Client { get; set; }

	[NonDefault, SkipValidation]
	public required IDto Dto { get; set; }

	[Required]
	public required string Service { get; set; }

	[Required]
	public required string Event { get; set; }
}
