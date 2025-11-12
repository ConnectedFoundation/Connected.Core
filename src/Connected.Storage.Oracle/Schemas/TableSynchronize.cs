namespace Connected.Storage.Oracle.Schemas;

/// <summary>
/// Synchronizes an Oracle table schema with the entity definition.
/// </summary>
/// <remarks>
/// This transaction represents the main entry point for table synchronization operations. It checks
/// whether the target table exists and delegates to the appropriate transaction class based on
/// the table state. For new tables, it creates them using <see cref="TableCreate"/>. For existing
/// tables, it loads the current schema from ALL_TAB_COLUMNS and ALL_CONSTRAINTS, then compares it
/// with the desired schema and delegates to <see cref="TableAlter"/> to apply necessary modifications.
/// This two-phase approach ensures efficient schema synchronization by only modifying tables when
/// differences are detected. Oracle automatically commits DDL operations, so each table operation
/// is atomic but cannot be rolled back.
/// </remarks>
internal sealed class TableSynchronize
	: TableTransaction
{
	/// <inheritdoc/>
	protected override async Task OnExecute()
	{
		/*
		 * Check if the table exists in the database by querying ALL_TABLES.
		 */
		if (!await new TableExists().Execute(Context))
		{
			/*
			 * Table doesn't exist, create it with all columns, constraints, and indexes.
			 */
			await new TableCreate(false).Execute(Context);
			return;
		}

		/*
		 * Table exists, load current schema from Oracle data dictionary.
		 * Create ExistingSchema instance and populate it with metadata.
		 */
		var existingSchema = new ExistingSchema
		{
			Schema = Context.Schema.Schema,
			Name = Context.Schema.Name,
			Type = Context.Schema.Type
		};

		await existingSchema.Load(Context);

		Context.ExistingSchema = existingSchema;

		/*
		 * Compare existing schema with desired schema and apply alterations.
		 */
		await new TableAlter().Execute(Context);
	}
}
