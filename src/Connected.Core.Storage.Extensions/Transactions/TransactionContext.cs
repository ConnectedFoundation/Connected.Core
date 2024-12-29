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

	public bool IsDirty { get; set; }

	private void ChangeState(MiddlewareTransactionState state)
	{
		if (State == state)
			return;

		State = state;
		StateChanged?.Invoke(this, EventArgs.Empty);
	}

	public void Register(ITransactionClient client)
	{
		if (client is null || Operations.Contains(client))
			return;

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
				if (op is not null)
					await op.Commit();
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
				if (op is not null)
					await op.Rollback();
			}
		}

		ChangeState(MiddlewareTransactionState.Completed);
	}
}