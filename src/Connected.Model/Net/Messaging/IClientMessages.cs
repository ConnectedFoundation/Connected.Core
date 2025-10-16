using Connected.Net.Dtos;
using System.Collections.Immutable;

namespace Connected.Net.Messaging;

public interface IClientMessages<TMessage>
	where TMessage : IMessage
{
	IImmutableList<TMessage> Dequeue();

	void Clean();
	void Remove(string connectionId);
	void Remove(string connection, IMessageAcknowledgeDto dto);
	void Remove(string connection, string key);
	void Add(string client, TMessage message);
}
