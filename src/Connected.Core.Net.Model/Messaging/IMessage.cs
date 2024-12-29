namespace Connected.Net.Messaging;

public interface IMessage<TDto>
{
	IClient Client { get; }
	ulong Id { get; }
	string? Key { get; }
	TDto? Dto { get; }
	DateTime NextVisible { get; set; }
	DateTime Expire { get; }
}
