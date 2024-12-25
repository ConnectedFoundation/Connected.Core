namespace Connected.Net.Hubs;

public interface IServer<TDto>
{
	event EventHandler<TDto>? Received;

	IClientMessages<TDto> Messages { get; }
	IClients Clients { get; }

	Task Send(ISendContextDto context, TDto dto);
	Task Receive(string method, TDto dto);
	Task Acknowledge(string connection, IMessageAcknowledgeDto dto);
}
