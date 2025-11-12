namespace Connected.Storage.PostgreSql.Schemas;

/// <summary>
/// Base class for PostgreSQL table-related schema transactions.
/// </summary>
/// <remarks>
/// This abstract class provides common functionality for all table modification operations
/// including table creation, alteration, and dropping. It extends <see cref="SynchronizationTransaction"/>
/// and inherits all schema command capabilities such as identifier escaping and type formatting.
/// Derived classes implement specific table operations by overriding the OnExecute method to
/// generate and execute appropriate DDL statements for their respective operations.
/// </remarks>
internal abstract class TableTransaction
	: SynchronizationTransaction
{
}
