using Connected.Authentication;
using Connected.Identities;
using Connected.Net.Http;
using Microsoft.AspNetCore.SignalR;

namespace Connected.Net.Messaging;

public abstract class ServerHub(IClients clients)
	: Hub
{
	public override async Task OnConnectedAsync()
	{
		await HubAuthentication.Authenticate(Context, clients);
		/*
		 * Reliable messaging is based on a static client id which must be passed
		 * when connecting. If the connection is lost and the client reconnects,
		 * the connection gets new id but the client remains the same thus enabling
		 * to redirect undelivered messages to the new connection.
		 */
		var clientId = Context.GetHttpContext()?.Request.Headers["client"].ToString();
		string? identityToken = null;

		if (string.IsNullOrEmpty(clientId))
		{
			clientId = Context.GetHttpContext()?.Request.Query["client"].ToString();

			if (string.IsNullOrWhiteSpace(clientId))
				throw new NullReferenceException("'client' header expected.");
		}

		if (!Guid.TryParse(clientId, out Guid id))
			throw new InvalidOperationException("'client' header is not a valid GUID.");

		await HubAuthentication.Authenticate(Context, clients);

		var ctx = Context.GetHttpContext();

		if (ctx is not null && await ctx.ResolveIdentity() is IIdentity identity)
			identityToken = identity.Token;

		clients.AddOrUpdate(CreateClient(Context.ConnectionId, id, identityToken));

		await base.OnConnectedAsync();
	}

	private static Client CreateClient(string connection, Guid id, string? identity)
	{
		return new Client
		{
			Connection = connection,
			Identity = identity,
			Id = id
		};
	}
}
