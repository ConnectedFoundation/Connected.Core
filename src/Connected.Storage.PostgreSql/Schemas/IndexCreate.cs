namespace Connected.Storage.PostgreSql.Schemas;

/// <summary>
/// Creates an index on a PostgreSQL database table.
/// </summary>
/// <remarks>
/// This transaction generates and executes the CREATE INDEX or CREATE UNIQUE INDEX DDL statement
/// to add an index to a table. The operation supports both regular and unique indexes, as well as
/// composite indexes spanning multiple columns. PostgreSQL creates B-tree indexes by default which
/// are suitable for most use cases. The index name is derived from the index descriptor to ensure
/// consistency with the schema definition. Index creation improves query performance for columns
/// frequently used in WHERE clauses, JOIN conditions, or ORDER BY operations.
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
		 */
		var columns = string.Join(", ", Descriptor.Columns.Select(c => Escape(c.Name)));

		/*
		 * Determine if this is a unique index
		 */
		var unique = Descriptor.IsUnique ? "UNIQUE " : string.Empty;

		/*
		 * Execute CREATE INDEX statement
		 */
		await Context.Execute($"CREATE {unique}INDEX {Escape(Descriptor.Name)} ON {Escape(Descriptor.Schema, Descriptor.Table)} ({columns})");
	}
}
