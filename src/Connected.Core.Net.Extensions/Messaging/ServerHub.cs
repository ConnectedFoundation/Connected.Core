using Connected.Identities;
using Connected.Services;
using Microsoft.AspNetCore.SignalR;

namespace Connected.Net.Messaging;

public abstract class ServerHub<TDto>(IServer<TDto> server) : Hub<IMessageHub<TDto>>
	where TDto : IDto
{
	protected IServer<TDto> Server { get; } = server;
	/// <summary>
	/// This method is called by the client connection
	/// </summary>
	/// <param name="dto"></param>
	/// <returns></returns>
	public async Task Notify(string method, TDto dto)
	{
		await Server.Receive(method, dto);
	}
	/// <summary>
	/// This method is called by the client connection
	/// </summary>
	/// <param name="dto"></param>
	/// <returns></returns>
	public async Task Acknowledge(IMessageAcknowledgeDto dto)
	{
		await Server.Acknowledge(Context.ConnectionId, dto);
	}

	public override async Task OnConnectedAsync()
	{
		/*
		 * Reliable messaging is based on a static client id which must be passed
		 * when connecting. If the connection is lost and the client reconnects,
		 * the connection gets new id but the client remains the same thus enabling
		 * to redirect undelivered messages to the new connection.
		 */
		var clientId = Context.GetHttpContext()?.Request.Query["client"].ToString();

		if (string.IsNullOrEmpty(clientId))
			throw new NullReferenceException("id query parameter expected.");

		var userId = 0L;
		var ctx = Context.GetHttpContext();

		if (ctx is not null && await ctx.ResolveUser() is IUser user)
			userId = user.Id;

		Server.Clients.AddOrUpdate(CreateClient(Context.ConnectionId, clientId, userId));

		await base.OnConnectedAsync();
	}

	protected virtual IClient CreateClient(string connection, string id, long user)
	{
		return new Client
		{
			Connection = connection,
			User = user,
			Id = id
		};
	}
}
