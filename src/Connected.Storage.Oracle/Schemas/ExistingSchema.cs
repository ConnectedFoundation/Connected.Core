using Connected.Storage.Schemas;

namespace Connected.Storage.Oracle.Schemas;

/// <summary>
/// Represents the existing schema structure loaded from an Oracle database.
/// </summary>
/// <remarks>
/// This class encapsulates the metadata of an existing database table including its
/// descriptor containing columns, constraints, and indexes. It is used during schema
/// synchronization to compare the current database state with the desired schema definition
/// and determine necessary modifications. The nullable descriptor indicates whether the
/// table exists in the database. Oracle-specific features include uppercase identifier handling,
/// sequence tracking for identity columns (pre-12c), and comprehensive constraint metadata
/// from ALL_CONSTRAINTS and ALL_CONS_COLUMNS system views.
/// </remarks>
internal sealed class ExistingSchema
	: ISchema
{
	/// <inheritdoc/>
	public List<ISchemaColumn> Columns { get; } = [];

	/// <inheritdoc/>
	public required string Schema { get; set; }

	/// <inheritdoc/>
	public required string Name { get; set; }

	/// <inheritdoc/>
	public required string Type { get; set; }

	/// <inheritdoc/>
	public bool Ignore { get; set; }

	/// <summary>
	/// Gets or sets the object descriptor containing detailed metadata about the table.
	/// </summary>
	/// <value>
	/// The descriptor with index, constraint, and identity information, or <c>null</c> if not loaded.
	/// </value>
	/// <remarks>
	/// Oracle stores comprehensive metadata in the data dictionary including ALL_TABLES,
	/// ALL_TAB_COLUMNS, ALL_CONSTRAINTS, ALL_CONS_COLUMNS, ALL_INDEXES, and ALL_IND_COLUMNS.
	/// The descriptor aggregates this information for schema comparison and synchronization.
	/// </remarks>
	public ObjectDescriptor? Descriptor { get; set; }

	/// <summary>
	/// Gets all indexes defined on the table.
	/// </summary>
	/// <value>
	/// A list of unique index descriptors referenced by the table's columns.
	/// </value>
	/// <remarks>
	/// Oracle supports various index types including B-tree (default), bitmap, function-based,
	/// and domain indexes. This property returns all indexes defined on the table regardless
	/// of type.
	/// </remarks>
	public List<ObjectIndex> Indexes
	{
		get
		{
			if (Descriptor is null)
				return [];

			/*
			 * Extract unique indexes from the descriptor's index collection
			 */
			return [.. Descriptor.Indexes.GroupBy(g => g.Name).Select(s => s.First())];
		}
	}

	/// <summary>
	/// Loads the schema metadata from the Oracle database.
	/// </summary>
	/// <param name="context">The execution context containing connection and schema information.</param>
	/// <returns>A task representing the asynchronous load operation.</returns>
	/// <remarks>
	/// This method queries the Oracle data dictionary to retrieve all metadata about the table
	/// including columns, indexes, constraints, and identity settings. It populates the Columns
	/// collection and Descriptor property with the discovered information and applies indexing
	/// and constraint flags to the appropriate columns. Oracle's system views (ALL_*) provide
	/// comprehensive metadata that is parsed and organized into the schema structure.
	/// </remarks>
	public async Task Load(SchemaExecutionContext context)
	{
		/*
		 * Load column definitions from ALL_TAB_COLUMNS.
		 * The Columns query returns ExistingSchema which already has columns populated.
		 */
		var loadedSchema = await new Columns().Execute(context);

		/*
		 * Copy columns from loaded schema to this instance
		 */
		Columns.AddRange(loadedSchema.Columns);

		/*
		 * Copy descriptor if available
		 */
		if (loadedSchema.Descriptor is not null)
			Descriptor = loadedSchema.Descriptor;

		/*
		 * Load complete table metadata including additional constraints and indexes
		 */
		var metadata = await new ObjectMetadata(this).Execute(context);

		if (metadata is not null)
		{
			/*
			 * Merge metadata into descriptor
			 */
			if (Descriptor is null)
				Descriptor = metadata;
			else
			{
				/*
				 * Merge indexes and constraints if not already present
				 */
				foreach (var index in metadata.Indexes)
				{
					if (!Descriptor.Indexes.Any(i => string.Equals(i.Name, index.Name, StringComparison.OrdinalIgnoreCase)))
						Descriptor.Indexes.Add(index);
				}

				foreach (var constraint in metadata.Constraints)
				{
					if (!Descriptor.Constraints.Any(c => string.Equals(c.Name, constraint.Name, StringComparison.OrdinalIgnoreCase)))
						Descriptor.Constraints.Add(constraint);
				}
			}
		}

		if (Descriptor is null)
			return;

		/*
		 * Apply index flags to columns based on index definitions
		 */
		foreach (var index in Descriptor.Indexes)
		{
			foreach (var column in index.Columns)
			{
				var existing = Columns.FirstOrDefault(f => string.Equals(f.Name, column, StringComparison.OrdinalIgnoreCase));

				if (existing is ExistingColumn existingCol)
				{
					existingCol.IsIndex = true;
					existingCol.Index = index.Name;
					existingCol.IsUnique = index.IsUnique;
				}
			}
		}

		/*
		 * Apply constraint flags to columns
		 */
		foreach (var constraint in Descriptor.Constraints)
		{
			if (constraint.ConstraintType != ConstraintType.PrimaryKey)
				continue;

			foreach (var column in constraint.Columns)
			{
				var existing = Columns.FirstOrDefault(f => string.Equals(f.Name, column, StringComparison.OrdinalIgnoreCase));

				if (existing is ExistingColumn existingCol)
					existingCol.IsPrimaryKey = true;
			}
		}
	}

	/// <summary>
	/// Resolves all indexes that reference a specific column.
	/// </summary>
	/// <param name="column">The column name to search for.</param>
	/// <returns>A list of indexes that include the specified column.</returns>
	/// <remarks>
	/// This method is useful for determining which indexes need to be dropped before
	/// a column can be dropped or modified. Oracle requires dropping dependent indexes
	/// before column modifications in some scenarios.
	/// </remarks>
	public List<ObjectIndex> ResolveIndexes(string column)
	{
		if (Descriptor is null)
			return [];

		/*
		 * Find all indexes that contain the specified column
		 */
		return [.. Descriptor.Indexes.Where(w => w.Columns.Any(a => string.Equals(a, column, StringComparison.OrdinalIgnoreCase)))];
	}

	/// <inheritdoc/>
	public bool Equals(ISchema? other)
	{
		if (other is null)
			return false;

		/*
		 * Compare schema identity by schema name and table name
		 */
		return string.Equals(Schema, other.Schema, StringComparison.OrdinalIgnoreCase)
			&& string.Equals(Name, other.Name, StringComparison.OrdinalIgnoreCase);
	}
}
