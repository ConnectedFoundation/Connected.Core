using Connected.Net.Messaging.Dtos;
using Connected.Net.Messaging.Ops;
using Connected.Services;
using System.Collections.Immutable;

namespace Connected.Net.Messaging;

internal sealed class ServerExtension(IServiceProvider services)
		: Service(services), IServerExtensions
{
	public async Task<IImmutableList<IClient>> ResolveConnections(ISendContextDto dto, IClients clients)
	{
		var op = GetOperation<ResolveConnections>();

		op.Clients = clients;

		return await Invoke(op, dto);
	}
}