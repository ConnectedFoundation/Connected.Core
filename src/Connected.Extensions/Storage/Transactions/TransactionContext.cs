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

			if (client is IStorageTransactionClient storage)
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
		foreach (var storage in TakeStorage())
		{
			try
			{
				await storage.Commit();
			}
			catch { }
		}

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

	private List<IStorageTransactionClient> TakeStorage()
	{
		lock (Lock)
		{
			var result = Storage.ToList();

			Storage.Clear();

			return result;
		}
	}

	public async Task Rollback()
	{
		if (State != MiddlewareTransactionState.Active)
			return;

		ChangeState(MiddlewareTransactionState.Reverting);

		foreach (var storage in TakeStorage())
		{
			try
			{
				await storage.Rollback();
			}
			catch { }
		}

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