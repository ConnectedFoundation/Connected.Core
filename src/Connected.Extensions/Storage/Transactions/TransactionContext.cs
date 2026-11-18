using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;

namespace Connected.Storage.Transactions;

internal class TransactionContext(ILogger<TransactionContext>? logger = null) : ITransactionContext
{
	public event EventHandler? StateChanged;

	// Read by other threads without the lock, so always the latest value
	private volatile MiddlewareTransactionState _state = MiddlewareTransactionState.Active;

	public MiddlewareTransactionState State => _state;

	private ConcurrentStack<ITransactionClient> Operations { get; } = new();
	private object Lock = new object();
	private HashSet<ITransactionClient> OperationsHashSet = new();

	public bool IsDirty { get; set; }

	public Guid Id { get; } = Guid.NewGuid();

	/*
	 * Leaves the active state for the one asked, once: a commit and a rollback racing each other can't both proceed.
	 */
	private bool Leave(MiddlewareTransactionState state)
	{
		lock (Lock)
		{
			if (State != MiddlewareTransactionState.Active)
				return false;

			_state = state;
		}

		RaiseStateChanged();

		return true;
	}

	private void Complete()
	{
		_state = MiddlewareTransactionState.Completed;

		RaiseStateChanged();
	}

	private void RaiseStateChanged()
	{
		if (StateChanged is not EventHandler handlers)
			return;

		/*
		 * Each handler is called on its own, so one that throws doesn't keep the ones after it from hearing of the change:
		 * the storage connections and caches subscribed to it commit or discard their work on it. A handler that fails
		 * is logged.
		 */
		foreach (EventHandler handler in handlers.GetInvocationList())
		{
			try
			{
				handler(this, EventArgs.Empty);
			}
			catch (Exception ex)
			{
				logger?.LogError(ex, "Transaction state handler {Handler} failed on {State}.", handler.Target?.GetType().FullName ?? handler.Method.Name, State);
			}
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
		if (!Leave(MiddlewareTransactionState.Committing))
			return;

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
				catch (Exception ex)
				{
					logger?.LogError(ex, "Transaction client {Client} failed to commit.", op.GetType().FullName);
				}
			}
		}

		Complete();
	}

	public async Task Rollback()
	{
		if (!Leave(MiddlewareTransactionState.Reverting))
			return;

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
				catch (Exception ex)
				{
					logger?.LogError(ex, "Transaction client {Client} failed to roll back.", op.GetType().FullName);
				}
			}
		}

		Complete();
	}
}