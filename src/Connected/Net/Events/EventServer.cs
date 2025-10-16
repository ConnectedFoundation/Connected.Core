using Connected.Net.Dtos;
using Connected.Net.Events.Dtos;
using Connected.Net.Events.Ops;
using Connected.Services;

namespace Connected.Net.Events;

internal sealed class EventServer(IServiceProvider services)
	: Service(services), IEventServer
{
	public async Task Acknowledge(IBoundMessageAcknowledgeDto dto)
	{
		await Invoke(GetOperation<Acknowledge>(), dto);
	}

	public async Task Send(ISendEventDto dto)
	{
		await Invoke(GetOperation<Send>(), dto);
	}

	public async Task Subscribe(ISubscribeEventDto dto)
	{
		await Invoke(GetOperation<Subscribe>(), dto);
	}

	public async Task Unsubscribe(IUnsubscribeEventDto dto)
	{
		await Invoke(GetOperation<Unsubscribe>(), dto);
	}
}
