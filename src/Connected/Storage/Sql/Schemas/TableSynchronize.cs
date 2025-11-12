using Connected.Storage.Schemas;

namespace Connected.Storage.Sql.Schemas;

/// <summary>
/// Synchronizes a database table with its target schema definition.
/// </summary>
/// <remarks>
/// This transaction serves as the main entry point for table schema synchronization operations.
/// It determines whether a table needs to be created, altered, or recreated based on the
/// current database state and target schema definition. The operation first checks for table
/// existence, then loads existing schema metadata if the table exists, and finally chooses
/// the appropriate synchronization strategy: table creation for new tables, incremental
/// alteration for simple changes, or full recreation for complex structural changes like
/// identity column modifications. The decision logic ensures data preservation while applying
/// schema changes efficiently based on the nature of the modifications required.
/// </remarks>
internal class TableSynchronize
	: TableTransaction
{
	private ExistingSchema? _existingSchema = null;

	/// <summary>
	/// Gets or sets a value indicating whether the table exists in the database.
	/// </summary>
	private bool TableExists { get; set; }

	/// <inheritdoc/>
	protected override async Task OnExecute()
	{
		/*
		 * Check if the table already exists in the database.
		 */
		TableExists = await new TableExists().Execute(Context);

		/*
		 * If table doesn't exist, create it with the target schema.
		 */
		if (!TableExists)
		{
			await new TableCreate(false).Execute(Context);
			return;
		}

		/*
		 * Load the existing schema metadata from the database.
		 */
		_existingSchema = new ExistingSchema
		{
			Name = Context.Schema.Name,
			Schema = Context.Schema.Schema,
			Type = Context.Schema.Type
		};

		await _existingSchema.Load(Context);

		Context.ExistingSchema = _existingSchema;

		/*
		 * Choose the appropriate synchronization strategy based on the type of changes required.
		 */
		if (ShouldRecreate)
			await new TableRecreate(_existingSchema).Execute(Context);
		else if (ShouldAlter)
			await new TableAlter(_existingSchema).Execute(Context);
	}

	/// <summary>
	/// Gets a value indicating whether the table should be altered.
	/// </summary>
	/// <value>
	/// <c>true</c> if the table structure differs from the target schema; otherwise, <c>false</c>.
	/// </value>
	private bool ShouldAlter => !Context.Schema.Equals(ExistingSchema);

	/// <summary>
	/// Gets a value indicating whether the table should be recreated.
	/// </summary>
	/// <value>
	/// <c>true</c> if identity settings or critical metadata have changed; otherwise, <c>false</c>.
	/// </value>
	/// <remarks>
	/// Table recreation is required when changes cannot be accomplished through ALTER statements,
	/// such as adding or removing identity properties or changing column data types.
	/// </remarks>
	private bool ShouldRecreate => HasIdentityChanged || HasColumnMetadataChanged;

	/// <summary>
	/// Gets the existing schema loaded from the database.
	/// </summary>
	private ExistingSchema? ExistingSchema => _existingSchema;

	/// <summary>
	/// Gets a value indicating whether any identity column settings have changed.
	/// </summary>
	/// <value>
	/// <c>true</c> if identity properties differ between existing and target schemas; otherwise, <c>false</c>.
	/// </value>
	/// <remarks>
	/// Identity changes require table recreation as they cannot be applied through ALTER statements.
	/// </remarks>
	private bool HasIdentityChanged
	{
		get
		{
			/*
			 * Check if any target columns with identity have no corresponding existing column.
			 */
			foreach (var column in Context.Schema.Columns)
			{
				if (ExistingSchema!.Columns.FirstOrDefault(f => string.Equals(f.Name, column.Name, StringComparison.OrdinalIgnoreCase)) is not ISchemaColumn existing)
					return true;

				if (existing.IsIdentity != column.IsIdentity)
					return true;
			}

			/*
			 * Check if any existing identity columns are being removed or modified.
			 */
			foreach (var existing in ExistingSchema!.Columns)
			{
				var column = Context.Schema.Columns.FirstOrDefault(f => string.Equals(f.Name, existing.Name, StringComparison.OrdinalIgnoreCase));

				if (column is null && existing.IsIdentity)
					return true;
				else if (column is not null && column.IsIdentity != existing.IsIdentity)
					return true;
			}

			return false;
		}
	}

	/// <summary>
	/// Gets a value indicating whether any column metadata has changed that requires recreation.
	/// </summary>
	/// <value>
	/// <c>true</c> if critical column properties differ; otherwise, <c>false</c>.
	/// </value>
	/// <remarks>
	/// Certain metadata changes like data type modifications require table recreation to ensure
	/// proper data conversion and constraint enforcement.
	/// </remarks>
	private bool HasColumnMetadataChanged
	{
		get
		{
			/*
			 * Compare each existing column's metadata with the target column definition.
			 */
			foreach (var existing in ExistingSchema!.Columns)
			{
				if (Context.Schema.Columns.FirstOrDefault(f => string.Equals(f.Name, existing.Name, StringComparison.OrdinalIgnoreCase)) is not ISchemaColumn column)
					continue;

				/*
				 * Check if any critical properties have changed that would require recreation.
				 */
				if (column.DataType != existing.DataType
					|| column.MaxLength != existing.MaxLength
					|| column.IsNullable != existing.IsNullable
					|| column.IsVersion != existing.IsVersion
					|| column.Precision != existing.Precision
					|| column.Scale != existing.Scale
					|| column.DateKind != existing.DateKind
					|| column.BinaryKind != existing.BinaryKind
					|| column.DatePrecision != existing.DatePrecision)
					return true;
			}

			return false;
		}
	}
}
