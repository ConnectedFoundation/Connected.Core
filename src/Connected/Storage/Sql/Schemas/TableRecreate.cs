namespace Connected.Storage.Sql.Schemas;

/// <summary>
/// Recreates a database table by creating a temporary table, copying data, and replacing the original.
/// </summary>
/// <remarks>
/// This transaction orchestrates a complex table recreation process used when table structure changes
/// cannot be accomplished through ALTER TABLE statements. The operation creates a temporary table with
/// the new structure, copies data from the existing table (with type conversions as needed), drops the
/// original table, and renames the temporary table to the original name. Special handling is provided
/// for identity columns by temporarily enabling IDENTITY_INSERT to preserve identity values during the
/// data copy. After recreation, all constraints and indexes are reapplied to the new table structure.
/// This approach ensures data preservation while allowing structural changes that would otherwise require
/// table drops.
/// </remarks>
internal class TableRecreate(ExistingSchema existing)
	: TableTransaction
{
	/// <inheritdoc/>
	protected override async Task OnExecute()
	{
		/*
		 * Create a temporary table with the new structure.
		 */
		var add = new TableCreate(true);

		await add.Execute(Context);

		/*
		 * Add default constraints to the temporary table.
		 */
		await ExecuteDefaults(add.TemporaryName ?? throw new NullReferenceException(SR.ErrExpectedTemporaryName));

		/*
		 * Enable IDENTITY_INSERT if the table has an identity column to preserve identity values.
		 */
		if (HasIdentity)
			await new IdentityInsert(add.TemporaryName, true).Execute(Context);

		/*
		 * Copy data from the existing table to the temporary table.
		 */
		await new DataCopy(existing, add.TemporaryName).Execute(Context);

		/*
		 * Disable IDENTITY_INSERT after data copy is complete.
		 */
		if (HasIdentity)
			await new IdentityInsert(add.TemporaryName, false).Execute(Context);

		/*
		 * Drop the original table.
		 */
		await new TableDrop().Execute(Context);

		/*
		 * Rename the temporary table to the original table name.
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
	/// Gets a value indicating whether the table has an identity column.
	/// </summary>
	/// <value>
	/// <c>true</c> if the table contains a primary key column with identity property; otherwise, <c>false</c>.
	/// </value>
	private bool HasIdentity
	{
		get
		{
			foreach (var column in Context.Schema.Columns)
			{
				if (column.IsPrimaryKey && column.IsIdentity)
					return true;
			}

			return false;
		}
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
	/// Creates default value constraints for the specified table.
	/// </summary>
	/// <param name="tableName">The name of the table to add defaults to.</param>
	/// <returns>A task representing the asynchronous operation.</returns>
	private async Task ExecuteDefaults(string tableName)
	{
		/*
		 * Add default constraints for all columns that have default values defined.
		 */
		foreach (var column in Context.Schema.Columns)
		{
			if (!string.IsNullOrWhiteSpace(column.DefaultValue))
				await new DefaultAdd(column, tableName).Execute(Context);
		}
	}

	/// <summary>
	/// Creates indexes on the recreated table.
	/// </summary>
	/// <returns>A task representing the asynchronous operation.</returns>
	private async Task ExecuteIndexes()
	{
		var indexes = ParseIndexes(Context.Schema);

		foreach (var index in indexes)
			await new IndexCreate(index).Execute(Context);
	}
}
