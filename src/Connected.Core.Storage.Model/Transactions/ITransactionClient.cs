namespace Connected.Storage.Transactions;

public interface ITransactionClient
{
	Task Commit();
	Task Rollback();
}
