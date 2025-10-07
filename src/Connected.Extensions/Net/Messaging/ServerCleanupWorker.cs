using Connected.Runtime;
using Connected.Services;
using Microsoft.AspNetCore.SignalR;

namespace Connected.Net.Messaging;

public abstract class ServerCleanupWorker<TDto, THub> : ScheduledWorker
	where THub : Hub
{
	protected ServerCleanupWorker(IServer<TDto> server, IHubContext<THub> hub)
	{
		Server = server;
		Hub = hub;
		Timer = TimeSpan.FromMilliseconds(500);
	}

	protected IServer<TDto> Server { get; }
	private IHubContext<THub> Hub { get; }

	protected override async Task OnInvoke(CancellationToken cancellationToken)
	{
		await Send(cancellationToken);
		/*
		 * Clean up every 15 seconds 
		 */
		if (Count % 30 == 0)
			await Clean(cancellationToken);
	}

	private async Task Send(CancellationToken cancellationToken)
	{
		var messages = Server.Messages.Dequeue();

		foreach (var item in messages)
		{
			var dto = Dto.Factory.Create<IMessageAcknowledgeDto>();

			dto.Id = item.Id;

			var args = new List<object>
			{
				dto
			};

			if (item.Dto is not null)
				args.Add(item.Dto);

			await Hub.Clients.Client(item.Client.Connection).SendCoreAsync("Notify", args.ToArray(), cancellationToken);
		}
	}

	private async Task Clean(CancellationToken cancellationToken)
	{
		Server.Messages.Clean();
		Server.Clients.Clean();

		await Task.CompletedTask;
	}
}
