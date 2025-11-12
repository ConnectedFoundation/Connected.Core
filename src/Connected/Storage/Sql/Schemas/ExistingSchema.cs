using Connected.Storage.Schemas;

namespace Connected.Storage.Sql.Schemas;

/// <summary>
/// Represents a schema definition loaded from an existing database table.
/// </summary>
/// <remarks>
/// This class encapsulates the complete metadata for a table that already exists in the database.
/// It implements <see cref="ISchema"/> and is populated by querying database system catalogs
/// during schema discovery operations. The Load method orchestrates the retrieval of column
/// definitions, index information, constraint details, and identity column configuration from
/// the database. This existing schema representation is compared against entity-based schema
/// definitions to determine what synchronization operations are needed. The class provides
/// specialized methods for resolving indexes and organizing constraint information.
/// </remarks>
internal class ExistingSchema
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
	/// Gets the object descriptor containing detailed metadata about the table.
	/// </summary>
	/// <value>
	/// The descriptor with index, constraint, and identity information, or <c>null</c> if not loaded.
	/// </value>
	public ObjectDescriptor? Descriptor { get; private set; }

	/// <summary>
	/// Loads the schema metadata from the database.
	/// </summary>
	/// <param name="context">The execution context containing connection and schema information.</param>
	/// <returns>A task representing the asynchronous load operation.</returns>
	/// <remarks>
	/// This method queries the database system catalogs to retrieve all metadata about the table
	/// including columns, indexes, constraints, and identity settings. It populates the Columns
	/// collection and Descriptor property with the discovered information and applies indexing
	/// and constraint flags to the appropriate columns.
	/// </remarks>
	public async Task Load(SchemaExecutionContext context)
	{
		Name = context.Schema.Name;
		Type = context.Schema.Type;
		Schema = context.Schema.Schema;

		/*
		 * Load column definitions from the database.
		 */
		Columns.AddRange(await new Columns(this).Execute(context));

		/*
		 * Load detailed object metadata including indexes and constraints.
		 */
		Descriptor = await new SpHelp().Execute(context);

		/*
		 * Mark the identity column if one exists.
		 */
		if (Columns.FirstOrDefault(f => string.Equals(f.Name, Descriptor.Identity.Identity, StringComparison.OrdinalIgnoreCase)) is ExistingColumn c)
			c.IsIdentity = true;

		/*
		 * Apply index information to columns based on the descriptor metadata.
		 */
		foreach (var index in Descriptor.Indexes)
		{
			foreach (var column in index.Columns)
			{
				if (Columns.FirstOrDefault(f => string.Equals(column, f.Name, StringComparison.OrdinalIgnoreCase)) is not ExistingColumn col)
					continue;

				/*
				 * Set appropriate index flags based on the index type.
				 */
				switch (index.Type)
				{
					case IndexType.Index:
						col.IsIndex = true;
						break;
					case IndexType.Unique:
						col.IsIndex = true;
						col.IsUnique = true;
						break;
					case IndexType.PrimaryKey:
						col.IsPrimaryKey = true;
						col.IsIndex = true;
						col.IsUnique = true;
						break;
				}
			}
		}

		/*
		 * Apply constraint information to columns based on the descriptor metadata.
		 */
		foreach (var constraint in Descriptor.Constraints)
		{
			switch (constraint.ConstraintType)
			{
				case ConstraintType.Default:
					if (Columns.FirstOrDefault(f => string.Equals(f.Name, constraint.Columns[0], StringComparison.OrdinalIgnoreCase)) is ExistingColumn column)
						column.DefaultValue = constraint.DefaultValue;
					break;
			}
		}
	}

	/// <summary>
	/// Gets all indexes defined on the table.
	/// </summary>
	/// <value>
	/// A list of unique index descriptors referenced by the table's columns.
	/// </value>
	public List<ObjectIndex> Indexes
	{
		get
		{
			var result = new List<ObjectIndex>();

			/*
			 * Collect unique indexes from all columns to avoid duplicates.
			 */
			foreach (var column in Columns)
			{
				var indexes = ResolveIndexes(column.Name);

				foreach (var index in indexes)
				{
					if (result.FirstOrDefault(f => string.Equals(f.Name, index.Name, StringComparison.OrdinalIgnoreCase)) is null)
						result.Add(index);
				}
			}

			return result;
		}
	}

	/// <summary>
	/// Resolves all indexes that reference a specific column.
	/// </summary>
	/// <param name="column">The column name to search for.</param>
	/// <returns>A list of indexes that include the specified column.</returns>
	/// <remarks>
	/// This method is useful for determining which indexes need to be dropped before
	/// a column can be dropped or modified.
	/// </remarks>
	public List<ObjectIndex> ResolveIndexes(string column)
	{
		var result = new List<ObjectIndex>();

		if (Descriptor is not null)
		{
			/*
			 * Find all indexes that reference the specified column.
			 */
			foreach (var index in Descriptor.Indexes)
			{
				if (index.IsReferencedBy(column))
					result.Add(index);
			}
		}

		return result;
	}

	/// <inheritdoc/>
	public bool Equals(ISchema? other)
	{
		throw new NotImplementedException();
	}
}
