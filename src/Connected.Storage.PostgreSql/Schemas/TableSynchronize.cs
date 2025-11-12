namespace Connected.Storage.PostgreSql.Schemas;

/// <summary>
/// Synchronizes a PostgreSQL table schema with the entity definition.
/// </summary>
/// <remarks>
/// This transaction represents the main entry point for table synchronization operations. It checks
/// whether the target table exists and delegates to the appropriate transaction class based on
/// the table state. For new tables, it creates them using <see cref="TableCreate"/>. For existing
/// tables, it loads the current schema and compares it with the desired schema, then delegates to
/// <see cref="TableAlter"/> to apply necessary modifications. This two-phase approach ensures
/// efficient schema synchronization by only modifying tables when differences are detected.
/// </remarks>
internal sealed class TableSynchronize
	: TableTransaction
{
	/// <inheritdoc/>
	protected override async Task OnExecute()
	{
		/*
		 * Check if the table exists in the database.
		 */
		if (!await new TableExists().Execute(Context))
		{
			/*
			 * Table doesn't exist, create it.
			 */
			await new TableCreate(false).Execute(Context);
			return;
		}

		/*
		 * Table exists, load current schema and compare with desired schema.
		 * If differences are found, alter the table structure.
		 */
		Context.ExistingSchema = await new Columns().Execute(Context);

		await new TableAlter().Execute(Context);
	}
}
