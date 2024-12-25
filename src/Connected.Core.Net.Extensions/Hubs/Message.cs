using System;
using System.Threading;

namespace Connected.Net.Hubs;

internal sealed class Message<TDto> : IMessage<TDto>
{
	private static ulong _identity = 0UL;

	public Message(IClient client, TDto? dto)
	{
		Client = client;
		Dto = dto;
		Id = Interlocked.Increment(ref _identity);
		Expire = DateTime.UtcNow.AddMinutes(5);
	}

	public IClient Client { get; }
	public ulong Id { get; }
	public string? Key { get; }
	public TDto? Dto { get; }
	public DateTime NextVisible { get; set; } = DateTime.UtcNow.AddSeconds(5);
	public DateTime Expire { get; }
}
