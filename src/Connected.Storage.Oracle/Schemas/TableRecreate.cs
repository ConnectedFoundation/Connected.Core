namespace Connected.Storage.Oracle.Schemas;

/// <summary>
/// Recreates an Oracle database table by creating a temporary table, copying data, and replacing the original.
/// </summary>
/// <remarks>
/// This transaction orchestrates a complex table recreation process used when table structure changes
/// cannot be accomplished through ALTER TABLE statements. The operation creates a temporary table with
/// the new structure, copies data from the existing table (with type conversions as needed), drops the
/// original table, and renames the temporary table to the original name. Oracle handles identity columns
/// differently than SQL Server - GENERATED AS IDENTITY columns (12c+) are automatically populated during
/// INSERT, and for earlier versions with sequences, the sequence continues from its current value.
/// After recreation, all constraints and indexes are reapplied to the new table structure. This approach
/// ensures data preservation while allowing structural changes that would otherwise require table drops.
/// Oracle automatically commits all DDL statements.
/// </remarks>
internal sealed class TableRecreate(ExistingSchema existing)
	: TableTransaction
{
	/// <inheritdoc/>
	protected override async Task OnExecute()
	{
		/*
		 * Create a temporary table with the new structure.
		 * Oracle temporary table naming uses a unique suffix.
		 */
		var add = new TableCreate(true);

		await add.Execute(Context);

		/*
		 * Copy data from the existing table to the temporary table.
		 * Oracle doesn't have IDENTITY_INSERT equivalent - identity columns
		 * are either GENERATED AS IDENTITY (12c+) which auto-populates,
		 * or managed by sequences which continue from current value.
		 */
		await new DataCopy(existing, add.TemporaryName ?? throw new NullReferenceException(Schemas.SR.ErrExpectedExpression)).Execute(Context);

		/*
		 * Drop the original table.
		 * Oracle automatically drops dependent indexes but may require
		 * CASCADE CONSTRAINTS for foreign keys.
		 */
		await new TableDrop().Execute(Context);

		/*
		 * Rename the temporary table to the original table name.
		 * Oracle uses ALTER TABLE old_name RENAME TO new_name syntax.
		 */
		await new TableRename(add.TemporaryName).Execute(Context);

		/*
		 * Recreate the primary key constraint on the new table.
		 */
		await ExecutePrimaryKey();

		/*
		 * Recreate all indexes on the new table.
		 */
		await ExecuteIndexes();
	}

	/// <summary>
	/// Creates the primary key constraint on the recreated table.
	/// </summary>
	/// <returns>A task representing the asynchronous operation.</returns>
	private async Task ExecutePrimaryKey()
	{
		var pk = Context.Schema.Columns.FirstOrDefault(f => f.IsPrimaryKey);

		if (pk != null)
			await new PrimaryKeyAdd(pk).Execute(Context);
	}

	/// <summary>
	/// Creates indexes on the recreated table.
	/// </summary>
	/// <returns>A task representing the asynchronous operation.</returns>
	/// <remarks>
	/// Oracle indexes include B-tree (default), bitmap, function-based, and domain indexes.
	/// All are created using CREATE INDEX or CREATE UNIQUE INDEX syntax.
	/// </remarks>
	private async Task ExecuteIndexes()
	{
		var indexes = ParseIndexes(Context.Schema);

		foreach (var index in indexes)
			await new IndexCreate(index).Execute(Context);
	}
}
