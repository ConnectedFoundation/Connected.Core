using Connected.Annotations;

namespace Connected.Storage.Transactions;

public enum MiddlewareTransactionState
{
	Active = 1,
	Committing = 2,
	Reverting = 3,
	Completed = 4
}

[Service]
public interface ITransactionContext
{
	event EventHandler? StateChanged;
	MiddlewareTransactionState State { get; }
	Guid Id { get; }
	void Register(ITransactionClient client);
	bool IsDirty { get; set; }

	Task Rollback();
	Task Commit();
}
