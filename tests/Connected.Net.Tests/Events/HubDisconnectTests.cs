using Connected.Net.Events;
using Connected.Net.Events.Dtos;
using Connected.Net.Events.Ops;
using Connected.Net.Messaging;

namespace Connected.Net.Tests.Events;

/// <summary>
/// Checks that a lost connection leaves its event subscriptions, and that a client reconnecting acts as whoever it
/// connected as the last time.
/// </summary>
[TestClass]
public sealed class HubDisconnectTests
{
	[TestMethod]
	public async Task UnsubscribingWithoutServiceAndEventLeavesEveryEvent()
	{
		var subscriptions = new EventSubscriptions();
		var gone = NewClient("gone", "a");
		var stays = NewClient("stays", "b");

		subscriptions.Items["services/a/updated"] = [gone, stays];
		subscriptions.Items["services/b/deleted"] = [gone];

		await new Unsubscribe(subscriptions).Invoke(new UnsubscribeEventDto { Connection = "GONE" });

		CollectionAssert.AreEqual(new[] { stays }, subscriptions.Items["services/a/updated"]);
		Assert.AreEqual(0, subscriptions.Items["services/b/deleted"].Count);
	}

	[TestMethod]
	public async Task UnsubscribingFromOneEventKeepsTheOthers()
	{
		var subscriptions = new EventSubscriptions();
		var client = NewClient("connection", "a");

		subscriptions.Items["services/a/updated"] = [client];
		subscriptions.Items["services/b/deleted"] = [client];

		await new Unsubscribe(subscriptions).Invoke(new UnsubscribeEventDto { Connection = "connection", Service = "services/a", Event = "updated" });

		Assert.AreEqual(0, subscriptions.Items["services/a/updated"].Count);
		Assert.AreEqual(1, subscriptions.Items["services/b/deleted"].Count);
	}

	[TestMethod]
	public void ClientReconnectingTakesItsNewIdentity()
	{
		var clients = new EventClients();
		var id = Guid.NewGuid();

		clients.AddOrUpdate(NewClient("first", "signed-out", id));
		clients.Select(id)!.RetentionDeadline = DateTime.UtcNow.AddMinutes(5);
		clients.AddOrUpdate(NewClient("second", "signed-in", id));

		var client = clients.Select(id)!;

		Assert.AreEqual("second", client.Connection);
		Assert.AreEqual("signed-in", client.Identity);
		Assert.AreEqual(DateTime.MinValue, client.RetentionDeadline);
	}

	[TestMethod]
	public void ClientNotReconnectingIsRemovedOnceItsRetentionEnds()
	{
		var clients = new EventClients();
		var id = Guid.NewGuid();

		clients.AddOrUpdate(NewClient("connection", "a", id));
		clients.Select(id)!.RetentionDeadline = DateTime.UtcNow.AddSeconds(-1);
		clients.Clean();

		Assert.IsNull(clients.Select(id));
	}

	private static IClient NewClient(string connection, string identity, Guid? id = null)
	{
		return new Client { Id = id ?? Guid.NewGuid(), Connection = connection, Identity = identity };
	}
}
