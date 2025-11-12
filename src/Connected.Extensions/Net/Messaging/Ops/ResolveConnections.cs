using Connected.Net.Messaging.Dtos;
using Connected.Services;
using System.Collections.Immutable;

namespace Connected.Net.Messaging.Ops;
internal sealed class ResolveConnections
	: ServiceFunction<ISendContextDto, IImmutableList<IClient>>
{
	public required IClients Clients { get; set; }

	protected override async Task<IImmutableList<IClient>> OnInvoke()
	{
		if (Dto.Filter == SendFilterFlags.None)
			return Clients.Query();

		var result = new List<IClient>();

		foreach (var client in Clients.Query())
		{
			if (Dto.Filter.HasFlag(SendFilterFlags.ExceptSender) && Dto.Connection is not null)
			{
				if (string.Equals(Dto.Connection, client.Connection, StringComparison.OrdinalIgnoreCase))
					continue;
			}

			if (Dto.Filter.HasFlag(SendFilterFlags.IdentityConnections) && !string.IsNullOrWhiteSpace(Dto.Identity))
			{
				if (string.Equals(client.Identity, Dto.Identity, StringComparison.Ordinal))
					continue;
			}

			result.Add(client);
		}

		return await Task.FromResult(result.ToImmutableList());
	}
}
