namespace Connected.Storage.Oracle.Schemas;

/// <summary>
/// Creates an index on an Oracle database table.
/// </summary>
/// <remarks>
/// This transaction generates and executes the CREATE INDEX or CREATE UNIQUE INDEX DDL statement
/// to add an index to a table. The operation supports both regular and unique indexes, as well as
/// composite indexes spanning multiple columns. Oracle creates B-tree indexes by default which
/// are suitable for most use cases. The index name is derived from the index descriptor to ensure
/// consistency with the schema definition. Index creation improves query performance for columns
/// frequently used in WHERE clauses, JOIN conditions, or ORDER BY operations. Oracle also supports
/// bitmap indexes for low-cardinality columns and function-based indexes for computed expressions.
/// Index names are limited to 30 characters (pre-12.2) or 128 characters (12.2+) and are
/// case-insensitive (stored uppercase unless quoted).
/// </remarks>
internal sealed class IndexCreate(IndexDescriptor descriptor)
	: SynchronizationTransaction
{
	private IndexDescriptor Descriptor { get; } = descriptor;

	/// <inheritdoc/>
	protected override async Task OnExecute()
	{
		/*
		 * Build the list of column names for the index
		 * Oracle uses double-quoted identifiers for case-sensitive column names
		 */
		var columns = string.Join(", ", Descriptor.Columns.Select(c => Escape(c)));

		/*
		 * Determine if this is a unique index
		 * Unique indexes prevent duplicate values in the indexed columns
		 */
		var unique = Descriptor.IsUnique ? "UNIQUE " : string.Empty;

		/*
		 * Execute CREATE INDEX statement
		 * Oracle automatically commits DDL statements
		 */
		await Context.Execute($"CREATE {unique}INDEX {Escape(Descriptor.Name)} ON {Escape(Descriptor.Schema, Descriptor.TableName)} ({columns})");
	}
}
