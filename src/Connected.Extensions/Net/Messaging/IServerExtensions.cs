using Connected.Annotations;
using Connected.Net.Messaging.Dtos;
using System.Collections.Immutable;

namespace Connected.Net.Messaging;

[Service]
public interface IServerExtensions
{
	Task<IImmutableList<IClient>> ResolveConnections(ISendContextDto dto, IClients clients);
}
