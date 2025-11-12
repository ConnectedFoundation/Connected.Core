using Connected.Storage.Schemas;

namespace Connected.Storage.PostgreSql.Schemas;

/// <summary>
/// Base class for PostgreSQL column-related schema transactions.
/// </summary>
/// <remarks>
/// This abstract class provides common functionality for all column modification operations
/// including adding, altering, and dropping columns. It extends <see cref="SynchronizationTransaction"/>
/// and maintains a reference to the column being operated on. Derived classes implement specific
/// column operations by overriding the OnExecute method to generate and execute appropriate DDL
/// statements for their respective operations.
/// </remarks>
internal abstract class ColumnTransaction(ISchemaColumn column)
	: SynchronizationTransaction
{
	/// <summary>
	/// Gets the column being operated on.
	/// </summary>
	protected ISchemaColumn Column { get; } = column;
}
