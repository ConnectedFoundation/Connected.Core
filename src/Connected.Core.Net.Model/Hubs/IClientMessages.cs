namespace Connected.Net.Hubs;

public interface IClientMessages<TDto>
{
	ImmutableList<IMessage<TDto>> Dequeue();

	void Clean();
	void Remove(string connectionId);
	void Remove(string connection, IMessageAcknowledgeDto dto);
	void Remove(string connection, string key);
	void Add(string client, IMessage<TDto> message);
}
