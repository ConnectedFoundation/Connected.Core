namespace Connected.Net.Events.Dtos;

public interface IUnsubscribeEventDto
	: IEventServerDto
{
	string? Service { get; set; }
	string? Event { get; set; }
}
