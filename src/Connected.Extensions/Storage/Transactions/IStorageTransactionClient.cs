namespace Connected.Storage.Transactions;

/// <summary>
/// A transaction client that holds the transaction's storage work.
/// </summary>
/// <remarks>
/// The transaction commits or rolls back these clients, in the order they were registered, before any other client,
/// and waits for them. The other clients, such as service operations raising their committed events, then run on data
/// that is already saved.
/// </remarks>
internal interface IStorageTransactionClient : ITransactionClient
{
}
