namespace Connected.Net.Events.Dtos;
internal sealed class UnsubscribeEventDto
	: EventServerDto, IUnsubscribeEventDto
{
	public string? Service { get; set; }

	public string? Event { get; set; }
}
