namespace Connected.Storage.Schemas;

/// <summary>
/// Represents an entity schema definition for database tables.
/// </summary>
/// <remarks>
/// This class encapsulates the complete schema metadata for an entity type, including
/// the table name, schema namespace, column definitions, and behavioral flags. It implements
/// the <see cref="ISchema"/> interface to provide schema comparison capabilities essential
/// for schema synchronization operations. The equality implementation performs deep comparison
/// of all schema properties including column-by-column comparison to detect structural changes.
/// This schema representation is used throughout the storage layer for DDL generation, schema
/// validation, and migration operations across different storage providers.
/// </remarks>
internal sealed class EntitySchema
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
	/// Determines whether the current schema is equal to another schema.
	/// </summary>
	/// <param name="other">The schema to compare with the current schema.</param>
	/// <returns>
	/// <c>true</c> if the specified schema is equal to the current schema; otherwise, <c>false</c>.
	/// </returns>
	/// <remarks>
	/// This method performs a comprehensive comparison of schema definitions including name,
	/// schema namespace, column count, and individual column equality. The comparison is
	/// ordinal and case-sensitive to ensure exact schema matching. All columns must match
	/// in order and structure for schemas to be considered equal.
	/// </remarks>
	public bool Equals(ISchema? other)
	{
		if (other is null)
			return false;

		/*
		 * Compare the table/object name with ordinal comparison for exact matching.
		 */
		if (!string.Equals(Name, other.Name, StringComparison.Ordinal))
			return false;

		/*
		 * Compare the schema namespace to ensure the object resides in the same logical grouping.
		 */
		if (!string.Equals(Schema, other.Schema, StringComparison.Ordinal))
			return false;

		/*
		 * Verify that both schemas have the same number of columns before comparing individual columns.
		 */
		if (Columns.Count != other.Columns.Count)
			return false;

		/*
		 * Perform column-by-column comparison to ensure structural equality.
		 * Each column must implement IEquatable<ISchemaColumn> for proper comparison.
		 */
		for (var i = 0; i < Columns.Count; i++)
		{
			if (Columns[i] is not IEquatable<ISchemaColumn> left || !left.Equals(other.Columns[i]))
				return false;
		}

		return true;
	}
}