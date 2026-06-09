using Connected.Authentication;
using Connected.Identities;
using Connected.Net.Http;
using Connected.Storage.Transactions;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.DependencyInjection;

namespace Connected.Net.Messaging;

public abstract class ServerHub(IServiceProvider services, IClients clients)
	: Hub
{
	public override async Task OnConnectedAsync()
	{
		try
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
			await Commit();
		}
		catch
		{
			await Rollback();

			throw;
		}
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

	protected async Task Rollback()
	{
		var ctx = services.GetService<ITransactionContext>();

		if (ctx != null)
			await ctx.Rollback();
	}

	protected async Task Commit()
	{
		var ctx = services.GetService<ITransactionContext>();

		if (ctx != null)
			await ctx.Commit();
	}
}
