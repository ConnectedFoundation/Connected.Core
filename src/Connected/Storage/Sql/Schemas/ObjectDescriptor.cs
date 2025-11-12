namespace Connected.Storage.Sql.Schemas;

/// <summary>
/// Represents the complete metadata descriptor for a database table object.
/// </summary>
/// <remarks>
/// This class aggregates all metadata information about a table obtained from SQL Server
/// system catalogs, typically through the sp_help stored procedure. It organizes metadata
/// into logical categories including filegroup information, ROWGUIDCOL settings, identity
/// column configuration, general object metadata, column definitions, indexes, and constraints.
/// This comprehensive descriptor is used during schema discovery and comparison operations
/// to build a complete picture of existing database structures. Each property represents a
/// different aspect of the table's structure and is populated by parsing the result sets
/// returned from system catalog queries.
/// </remarks>
internal class ObjectDescriptor
{
	/// <summary>
	/// Gets the filegroup information for the table.
	/// </summary>
	/// <value>
	/// An <see cref="ObjectFileGroup"/> instance containing filegroup details.
	/// </value>
	public ObjectFileGroup FileGroup { get; } = new();

	/// <summary>
	/// Gets the ROWGUIDCOL information for the table.
	/// </summary>
	/// <value>
	/// An <see cref="ObjectRowGuid"/> instance containing ROWGUIDCOL column name if applicable.
	/// </value>
	public ObjectRowGuid RowGuid { get; } = new();

	/// <summary>
	/// Gets the identity column information for the table.
	/// </summary>
	/// <value>
	/// An <see cref="ObjectIdentity"/> instance containing identity column configuration.
	/// </value>
	public ObjectIdentity Identity { get; } = new();

	/// <summary>
	/// Gets the general metadata for the table object.
	/// </summary>
	/// <value>
	/// An <see cref="ObjectMetaData"/> instance containing object name, owner, type, and creation date.
	/// </value>
	public ObjectMetaData MetaData { get; } = new();

	/// <summary>
	/// Gets the collection of column definitions for the table.
	/// </summary>
	/// <value>
	/// A list of <see cref="ObjectColumn"/> instances representing all columns in the table.
	/// </value>
	public List<ObjectColumn> Columns { get; } = [];

	/// <summary>
	/// Gets the collection of indexes defined on the table.
	/// </summary>
	/// <value>
	/// A list of <see cref="ObjectIndex"/> instances representing all indexes including primary keys.
	/// </value>
	public List<ObjectIndex> Indexes { get; } = [];

	/// <summary>
	/// Gets the collection of constraints defined on the table.
	/// </summary>
	/// <value>
	/// A list of <see cref="ObjectConstraint"/> instances representing default, unique, and primary key constraints.
	/// </value>
	public List<ObjectConstraint> Constraints { get; } = [];
}
