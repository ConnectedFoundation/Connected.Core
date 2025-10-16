using Connected.Services;

namespace Connected.Net.Messaging;

public interface IMessage
{
	IClient Client { get; }
	ulong Id { get; }
	string? Key { get; }
	IDto Dto { get; }
	DateTime NextVisible { get; set; }
	DateTime Expire { get; }
}
