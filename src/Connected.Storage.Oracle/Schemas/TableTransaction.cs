namespace Connected.Storage.Oracle.Schemas;

/// <summary>
/// Base class for Oracle table-related schema transactions.
/// </summary>
/// <remarks>
/// This abstract class provides common functionality for all table modification operations
/// including table creation, alteration, and dropping in Oracle databases. It extends <see cref="SynchronizationTransaction"/>
/// and inherits all schema command capabilities such as identifier escaping with double quotes
/// and Oracle data type formatting. Derived classes implement specific table operations by overriding
/// the OnExecute method to generate and execute appropriate DDL statements for their respective
/// operations. Oracle automatically commits DDL statements, so table operations cannot be rolled
/// back once executed. Table names are case-insensitive and stored in uppercase unless quoted
/// during creation.
/// </remarks>
internal abstract class TableTransaction
	: SynchronizationTransaction
{
}
