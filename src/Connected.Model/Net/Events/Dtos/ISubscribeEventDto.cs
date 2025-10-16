namespace Connected.Net.Events.Dtos;

public interface ISubscribeEventDto
	: IEventServerDto
{
	string Service { get; set; }
	string Event { get; set; }
}
