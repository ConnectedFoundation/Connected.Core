using System.Collections.Concurrent;

namespace Connected.Storage.Transactions;

internal class TransactionContext : ITransactionContext
{
	public event EventHandler? StateChanged;

	public TransactionContext()
	{
		Operations = new();
	}

	public MiddlewareTransactionState State { get; private set; } = MiddlewareTransactionState.Active;

	private ConcurrentStack<ITransactionClient> Operations { get; }
	private List<IStorageTransactionClient> Storage { get; } = [];
	private object Lock = new object();
	private HashSet<ITransactionClient> OperationsHashSet = new();

	public bool IsDirty { get; set; }

	public Guid Id { get; } = Guid.NewGuid();

	private void ChangeState(MiddlewareTransactionState state)
	{
		if (State == state)
			return;

		State = state;

		try
		{
			StateChanged?.Invoke(this, EventArgs.Empty);
		}
		catch { }
	}

	public void Register(ITransactionClient client)
	{
		if (client is null)
			return;

		lock (Lock)
		{
			if (!OperationsHashSet.Add(client))
				return;

			/*
			 * Storage registered once the commit or rollback has started is too late to go first; it is handled with the
			 * other clients, which are still being taken.
			 */
			if (client is IStorageTransactionClient storage && State == MiddlewareTransactionState.Active)
			{
				Storage.Add(storage);
				return;
			}
		}

		Operations.Push(client);
	}

	public async Task Commit()
	{
		if (State != MiddlewareTransactionState.Active)
			return;

		ChangeState(MiddlewareTransactionState.Committing);

		/*
		 * The data is saved before the other clients commit, so whatever they do once committed, such as
		 * raising events, sees it.
		 */
		await EndStorage(f => f.Commit());

		while (!Operations.IsEmpty)
		{
			if (Operations.TryPop(out ITransactionClient? op))
			{
				if (op is null)
					continue;

				try
				{
					await op.Commit();
				}
				catch { }
			}
		}

		ChangeState(MiddlewareTransactionState.Completed);
	}

	/*
	 * Commits or rolls back the storage registered so far, in the order it was registered.
	 */
	private async Task EndStorage(Func<IStorageTransactionClient, Task> end)
	{
		IStorageTransactionClient[] storage;

		lock (Lock)
		{
			storage = [.. Storage];
			Storage.Clear();
		}

		foreach (var client in storage)
		{
			try
			{
				await end(client);
			}
			catch { }
		}
	}

	public async Task Rollback()
	{
		if (State != MiddlewareTransactionState.Active)
			return;

		ChangeState(MiddlewareTransactionState.Reverting);

		await EndStorage(f => f.Rollback());

		while (!Operations.IsEmpty)
		{
			if (Operations.TryPop(out ITransactionClient? op))
			{
				if (op is null)
					continue;

				try
				{
					await op.Rollback();
				}
				catch { }
			}
		}

		ChangeState(MiddlewareTransactionState.Completed);
	}
}