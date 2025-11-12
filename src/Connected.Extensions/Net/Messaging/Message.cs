using Connected.Services;

namespace Connected.Net.Messaging;

public abstract class Message(IClient client, IDto dto)
		: IMessage
{
	private static ulong _identity = 0UL;

	public IClient Client { get; } = client;
	public ulong Id { get; } = Interlocked.Increment(ref _identity);
	public string? Key { get; protected set; }
	public IDto Dto { get; } = dto;
	public DateTime NextVisible { get; set; } = DateTime.UtcNow.AddSeconds(5);
	public DateTime Expire { get; protected set; } = DateTime.UtcNow.AddMinutes(5);
}
