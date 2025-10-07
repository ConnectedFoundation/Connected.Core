using System.Collections.Immutable;

namespace Connected.Net.Messaging;

public interface IClientMessages<TDto>
{
	IImmutableList<IMessage<TDto>> Dequeue();

	void Clean();
	void Remove(string connectionId);
	void Remove(string connection, IMessageAcknowledgeDto dto);
	void Remove(string connection, string key);
	void Add(string client, IMessage<TDto> message);
}
