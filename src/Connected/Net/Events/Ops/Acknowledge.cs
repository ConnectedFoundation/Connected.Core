using Connected.Net.Dtos;
using Connected.Net.Messaging.Dtos;
using Connected.Services;
using Microsoft.AspNetCore.SignalR;

namespace Connected.Net.Events.Ops;
internal sealed class Acknowledge(EventMessages messages, IHubContext<EventHub> hub)
	: ServiceAction<IBoundMessageAcknowledgeDto>
{
	protected override async Task OnInvoke()
	{
		try
		{
			messages.Remove(Dto.Connection, Dto);

			await Task.CompletedTask;
		}
		catch (Exception ex)
		{
			var exDto = new Dto<IServerExceptionDto>().Value;

			exDto.Message = ex.Message;

			await hub.Clients.AllExcept(Dto.Connection).SendCoreAsync("Exception", [exDto]);
		}
	}
}
