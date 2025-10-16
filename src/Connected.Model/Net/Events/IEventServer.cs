using Connected.Annotations;
using Connected.Net.Dtos;
using Connected.Net.Events.Dtos;

namespace Connected.Net.Events;

[Service]
public interface IEventServer
{
	Task Subscribe(ISubscribeEventDto dto);
	Task Unsubscribe(IUnsubscribeEventDto dto);
	Task Send(ISendEventDto dto);
	Task Acknowledge(IBoundMessageAcknowledgeDto dto);
}
