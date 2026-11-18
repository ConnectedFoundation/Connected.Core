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
}
