using Connected.Services;
using Microsoft.AspNetCore.SignalR;
using System.Collections.Immutable;

namespace Connected.Net.Hubs;

public abstract class Server<THub, TDto> : IServer<TDto>
	 where THub : Hub
{
	public event EventHandler<TDto>? Received;

	protected Server(IHubContext<THub> hub)
	{
		Messages = new ClientMessages<TDto>();
		Clients = new Clients();

		Hub = hub;
	}

	private IHubContext<THub> Hub { get; }

	public IClientMessages<TDto> Messages { get; }
	public IClients Clients { get; }

	public virtual async Task Send(ISendContextDto context, TDto dto)
	{
		var tasks = new List<Task>();

		foreach (var client in SelectConnections(context))
			tasks.Add(Send(context.Method, client, dto));

		await Task.WhenAll(tasks);
	}

	protected async Task Send(string method, IClient client, TDto dto)
	{
		var message = new Message<TDto>(client, dto);

		Messages.Add(client.Connection, message);

		var ack = await Scope.ResolveDto<IMessageAcknowledgeDto>();

		ack.Id = message.Id;

		await Hub.Clients.Client(client.Connection).SendCoreAsync(method, new object?[] { ack, dto });
	}

	public virtual Task Receive(string method, TDto dto)
	{
		Received?.Invoke(method, dto);

		return Task.CompletedTask;
	}

	public async Task Acknowledge(string connection, IMessageAcknowledgeDto dto)
	{
		try
		{
			Messages.Remove(connection, dto);

			await Task.CompletedTask;
		}
		catch (Exception ex)
		{
			var exDto = await Scope.ResolveDto<IServerExceptionDto>();

			exDto.Message = ex.Message;

			await Hub.Clients.AllExcept(connection).SendCoreAsync("Exception", [exDto]);
		}
	}

	private ImmutableList<IClient> SelectConnections(ISendContextDto context)
	{
		if (context.Filter == SendFilterFlags.None)
			return Clients.Query();

		var result = new List<IClient>();

		foreach (var client in Clients.Query())
		{
			if (context.Filter.HasFlag(SendFilterFlags.ExceptSender) && context.Connection is not null)
			{
				if (string.Equals(context.Connection, client.Connection, StringComparison.OrdinalIgnoreCase))
					continue;
			}

			if (context.Filter.HasFlag(SendFilterFlags.UserConnections) && context.User > 0)
			{
				if (client.User != context.User)
					continue;
			}

			result.Add(client);
		}

		return result.ToImmutableList();
	}
}