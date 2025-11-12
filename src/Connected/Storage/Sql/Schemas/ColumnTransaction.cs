using Connected.Storage.Schemas;

namespace Connected.Storage.Sql.Schemas;

/// <summary>
/// Base class for column-related schema transactions.
/// </summary>
/// <remarks>
/// This abstract class provides common functionality for operations that modify individual
/// columns within a table. It extends <see cref="TableTransaction"/> and adds a reference
/// to the specific column being operated on. Derived classes implement specific column
/// operations such as adding, altering, or dropping columns while leveraging the base
/// transaction infrastructure and column context provided by this class.
/// </remarks>
internal abstract class ColumnTransaction(ISchemaColumn column)
	: TableTransaction
{
	/// <summary>
	/// Gets the column being operated on.
	/// </summary>
	/// <value>
	/// The schema column definition for the target column.
	/// </value>
	protected ISchemaColumn Column { get; } = column;
}