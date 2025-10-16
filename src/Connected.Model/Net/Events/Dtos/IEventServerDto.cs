using Connected.Services;

namespace Connected.Net.Events.Dtos;

public interface IEventServerDto
	: IDto
{
	string Connection { get; set; }
}
