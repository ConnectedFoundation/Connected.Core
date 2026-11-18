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

	[TestMethod]
	public void ClientReconnectingAsTheSameIdentityStaysTheSameClient()
	{
		var clients = new EventClients();
		var id = Guid.NewGuid();
		var first = NewClient("first", "a", id);

		clients.AddOrUpdate(first);
		clients.AddOrUpdate(NewClient("second", "a", id));

		Assert.AreSame(first, clients.Select(id));
		Assert.AreEqual("second", first.Connection);
	}

	[TestMethod]
	public void ClientReconnectingAsSomeoneElseLeavesTheEarlierIdentityBehind()
	{
		var clients = new EventClients();
		var id = Guid.NewGuid();
		var earlier = NewClient("first", "signed-out", id);

		clients.AddOrUpdate(earlier);
		clients.AddOrUpdate(NewClient("second", "signed-in", id));

		Assert.AreNotSame(earlier, clients.Select(id));
		// What the earlier identity subscribed to still points at its own connection, not the new one
		Assert.AreEqual("first", earlier.Connection);
		Assert.AreEqual("signed-out", earlier.Identity);
	}

	[TestMethod]
	public void ClientReconnectingJustBeforeTheCleanupIsKept()
	{
		var clients = new EventClients();
		var id = Guid.NewGuid();

		clients.AddOrUpdate(NewClient("first", "a", id));
		clients.Select(id)!.RetentionDeadline = DateTime.UtcNow.AddSeconds(-1);
		clients.AddOrUpdate(NewClient("second", "a", id));
		clients.Clean();

		Assert.IsNotNull(clients.Select(id));
	}

	[TestMethod]
	public void RemovingAnOldConnectionKeepsTheClientThatReplacedIt()
	{
		var clients = new EventClients();
		var id = Guid.NewGuid();

		clients.AddOrUpdate(NewClient("first", "signed-out", id));
		clients.AddOrUpdate(NewClient("second", "signed-in", id));
		clients.Remove("first");

		Assert.AreEqual("second", clients.Select(id)?.Connection);
	}

	private static IClient NewClient(string connection, string identity, Guid? id = null)
	{
		return new Client { Id = id ?? Guid.NewGuid(), Connection = connection, Identity = identity };
	}
}
