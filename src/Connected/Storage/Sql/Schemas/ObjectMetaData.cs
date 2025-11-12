namespace Connected.Storage.Sql.Schemas;

/// <summary>
/// Represents general metadata for a database object.
/// </summary>
/// <remarks>
/// This class encapsulates basic object information obtained from SQL Server system catalogs.
/// It provides fundamental metadata about a database object including its name, ownership,
/// type classification, and creation timestamp. This information is primarily descriptive
/// and used for informational purposes during schema discovery and logging operations.
/// The object type can indicate whether it's a table, view, stored procedure, or other
/// database object type according to SQL Server's object type taxonomy.
/// </remarks>
internal class ObjectMetaData
{
	/// <summary>
	/// Gets or sets the object name.
	/// </summary>
	/// <value>
	/// The name of the database object.
	/// </value>
	public string? Name { get; set; }

	/// <summary>
	/// Gets or sets the object owner or schema.
	/// </summary>
	/// <value>
	/// The owner name or schema name that contains the object.
	/// </value>
	public string? Owner { get; set; }

	/// <summary>
	/// Gets or sets the object type.
	/// </summary>
	/// <value>
	/// The object type identifier as returned by system catalogs (e.g., "user table", "view").
	/// </value>
	public string? Type { get; set; }

	/// <summary>
	/// Gets or sets the creation date and time of the object.
	/// </summary>
	/// <value>
	/// The timestamp when the object was created in the database.
	/// </value>
	public DateTimeOffset Created { get; set; }
}
