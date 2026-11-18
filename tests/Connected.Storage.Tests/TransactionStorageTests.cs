using Connected.Storage.Transactions;

namespace Connected.Storage.Tests;

/// <summary>
/// Checks that a transaction waits for its storage to commit or roll back before its other clients do, so they act on
/// data already saved or discarded.
/// </summary>
[TestClass]
public sealed class TransactionStorageTests
{
	[TestMethod]
	public async Task OtherClientsCommitOnceTheStorageHasCommitted()
	{
		var context = new TransactionContext();
		var log = new List<string>();

		// Registered first, so an ordinary client would come last
		context.Register(new Storage("storage", log));
		context.Register(new Client("operation", log));

		await context.Commit();

		CollectionAssert.AreEqual(new[] { "storage committed", "operation committed" }, log);
	}

	[TestMethod]
	public async Task OtherClientsRollBackOnceTheStorageHasRolledBack()
	{
		var context = new TransactionContext();
		var log = new List<string>();

		// Registered first, so an ordinary client would come last
		context.Register(new Storage("storage", log));
		context.Register(new Client("operation", log));

		await context.Rollback();

		CollectionAssert.AreEqual(new[] { "storage rolled back", "operation rolled back" }, log);
	}

	[TestMethod]
	public async Task StorageIsCommittedInTheOrderItWasRegistered()
	{
		var context = new TransactionContext();
		var log = new List<string>();

		context.Register(new Storage("first", log));
		context.Register(new Storage("second", log));

		await context.Commit();

		CollectionAssert.AreEqual(new[] { "first committed", "second committed" }, log);
	}

	[TestMethod]
	public async Task StorageRegisteredDuringTheCommitIsStillCommitted()
	{
		var context = new TransactionContext();
		var log = new List<string>();

		context.Register(new Registering("operation", log, () => context.Register(new Storage("late storage", log))));

		await context.Commit();

		CollectionAssert.AreEqual(new[] { "operation committed", "late storage committed" }, log);
	}

	/*
	 * Documents today's rule: a storage client that fails is passed over, and the other clients still commit. What a
	 * failed storage commit should stop is a separate decision.
	 */
	[TestMethod]
	public async Task StorageThatFailsDoesNotStopTheOtherClients()
	{
		var context = new TransactionContext();
		var log = new List<string>();

		context.Register(new FailingStorage());
		context.Register(new Storage("storage", log));
		context.Register(new Client("operation", log));

		await context.Commit();

		CollectionAssert.AreEqual(new[] { "storage committed", "operation committed" }, log);
		Assert.AreEqual(MiddlewareTransactionState.Completed, context.State);
	}

	private sealed class FailingStorage : IStorageTransactionClient
	{
		public Task Commit() => throw new InvalidOperationException();

		public Task Rollback() => throw new InvalidOperationException();
	}

	/*
	 * Registers another client while it commits, as a first storage access during OnCommitted does.
	 */
	private sealed class Registering(string name, List<string> log, Action register) : ITransactionClient
	{
		public async Task Commit()
		{
			await Task.Yield();
			log.Add($"{name} committed");
			register();
		}

		public Task Rollback() => Task.CompletedTask;
	}

	private class Client(string name, List<string> log) : ITransactionClient
	{
		public async Task Commit()
		{
			await Task.Yield();
			log.Add($"{name} committed");
		}

		public async Task Rollback()
		{
			await Task.Yield();
			log.Add($"{name} rolled back");
		}
	}

	/*
	 * Yields before it records, as a real commit does on the database, so a transaction that didn't wait for it
	 * would let the other client run first.
	 */
	private sealed class Storage(string name, List<string> log) : Client(name, log), IStorageTransactionClient
	{
	}
}
