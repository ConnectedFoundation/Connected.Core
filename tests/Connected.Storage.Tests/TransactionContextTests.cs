using Connected.Storage.Transactions;

namespace Connected.Storage.Tests;

/// <summary>
/// Checks that every handler of a transaction's state changes hears of each change, whatever the others do.
/// </summary>
[TestClass]
public sealed class TransactionContextTests
{
	[TestMethod]
	public async Task HandlersAfterOneThatThrowsHearOfTheCommit()
	{
		var context = new TransactionContext();
		var heard = new List<MiddlewareTransactionState>();

		context.StateChanged += (_, _) => throw new InvalidOperationException();
		context.StateChanged += (_, _) => heard.Add(context.State);

		await context.Commit();

		CollectionAssert.AreEqual(new[] { MiddlewareTransactionState.Committing, MiddlewareTransactionState.Completed }, heard);
	}

	[TestMethod]
	public async Task HandlersAfterOneThatThrowsHearOfTheRollback()
	{
		var context = new TransactionContext();
		var heard = new List<MiddlewareTransactionState>();

		context.StateChanged += (_, _) => throw new InvalidOperationException();
		context.StateChanged += (_, _) => heard.Add(context.State);

		await context.Rollback();

		CollectionAssert.AreEqual(new[] { MiddlewareTransactionState.Reverting, MiddlewareTransactionState.Completed }, heard);
	}

	[TestMethod]
	public async Task HandlerThatThrowsStillHearsEveryChange()
	{
		var context = new TransactionContext();
		var heard = new List<MiddlewareTransactionState>();

		context.StateChanged += (_, _) =>
		{
			heard.Add(context.State);
			throw new InvalidOperationException();
		};

		await context.Commit();

		CollectionAssert.AreEqual(new[] { MiddlewareTransactionState.Committing, MiddlewareTransactionState.Completed }, heard);
	}

	[TestMethod]
	public async Task CommitAndRollbackRacingLeaveTheActiveStateOnce()
	{
		for (var i = 0; i < 200; i++)
		{
			var context = new TransactionContext();
			var heard = new System.Collections.Concurrent.ConcurrentQueue<MiddlewareTransactionState>();

			context.StateChanged += (_, _) => heard.Enqueue(context.State);

			using var start = new ManualResetEventSlim();
			var commit = Task.Run(async () => { start.Wait(); await context.Commit(); });
			var rollback = Task.Run(async () => { start.Wait(); await context.Rollback(); });

			start.Set();
			await Task.WhenAll(commit, rollback);

			Assert.AreEqual(2, heard.Count);
			Assert.AreEqual(MiddlewareTransactionState.Completed, heard.Last());
		}
	}
}
