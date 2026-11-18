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
	private object Lock = new object();
	private HashSet<ITransactionClient> OperationsHashSet = new();

	public bool IsDirty { get; set; }

	public Guid Id { get; } = Guid.NewGuid();

	private void ChangeState(MiddlewareTransactionState state)
	{
		if (State == state)
			return;

		State = state;

		if (StateChanged is not EventHandler handlers)
			return;

		/*
		 * Each handler is called on its own, so one that throws doesn't keep the ones after it from hearing of
		 * the change; storage and caches rely on it to commit or discard their work.
		 */
		foreach (var handler in handlers.GetInvocationList().Cast<EventHandler>())
		{
			try
			{
				handler(this, EventArgs.Empty);
			}
			catch { }
		}
	}

	public void Register(ITransactionClient client)
	{
		if (client is null)
			return;

		lock (Lock)
		{
			if (!OperationsHashSet.Add(client))
				return;
		}

		Operations.Push(client);
	}

	public async Task Commit()
	{
		if (State != MiddlewareTransactionState.Active)
			return;

		ChangeState(MiddlewareTransactionState.Committing);

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

	public async Task Rollback()
	{
		if (State != MiddlewareTransactionState.Active)
			return;

		ChangeState(MiddlewareTransactionState.Reverting);

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