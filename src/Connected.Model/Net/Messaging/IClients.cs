using System.Collections.Immutable;

namespace Connected.Net.Messaging;

public interface IClients
{
	void AddOrUpdate(IClient client);
	void Clean();
	void Remove(string connectionId);

	IImmutableList<IClient> Query();
	IClient? Select(Guid id);
	IClient? Select(string connection);
}
