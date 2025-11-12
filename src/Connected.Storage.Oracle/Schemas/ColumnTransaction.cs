using Connected.Storage.Schemas;

namespace Connected.Storage.Oracle.Schemas;

/// <summary>
/// Base class for Oracle column-related schema synchronization transactions.
/// </summary>
/// <remarks>
/// This abstract class provides common functionality for column-specific schema operations
/// such as adding, altering, or dropping columns. It holds a reference to the schema column
/// being operated on and provides access to the execution context through the base class.
/// Oracle column operations use ALTER TABLE statements and automatically commit upon execution.
/// Column names are case-insensitive and stored in uppercase unless quoted during creation.
/// Oracle has specific rules for column modifications including data type changes, nullability
/// changes, and default value assignments.
/// </remarks>
internal abstract class ColumnTransaction(ISchemaColumn column)
	: SynchronizationTransaction
{
	/// <summary>
	/// Gets the schema column for this transaction.
	/// </summary>
	/// <value>
	/// The <see cref="ISchemaColumn"/> containing column metadata including name, type, nullability,
	/// default values, and constraints.
	/// </value>
	protected ISchemaColumn Column { get; } = column;
}
