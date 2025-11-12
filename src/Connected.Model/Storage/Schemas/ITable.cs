namespace Connected.Storage.Schemas;

/// <summary>
/// Represents a database table schema with columns and indexes.
/// </summary>
/// <remarks>
/// This interface extends the base schema definition to provide table-specific metadata
/// including detailed column information and index definitions. Tables are the primary
/// data storage structures in relational databases, and this interface provides complete
/// schema information necessary for table creation, validation, and synchronization.
/// The table schema includes both the column structure (data types, constraints, properties)
/// and index definitions (which columns are indexed for performance optimization). This
/// comprehensive metadata enables schema comparison operations and DDL generation for
/// database migrations.
/// </remarks>
public interface ITable
	: ISchema
{
	/// <summary>
	/// Gets the collection of table column definitions.
	/// </summary>
	/// <value>
	/// A list containing detailed metadata for each column in the table.
	/// </value>
	/// <remarks>
	/// Table columns provide comprehensive information about each column's data type,
	/// constraints, and properties. This metadata is used for schema synchronization,
	/// validation, and generating DDL statements.
	/// </remarks>
	List<ITableColumn> TableColumns { get; }

	/// <summary>
	/// Gets the collection of indexes defined on this table.
	/// </summary>
	/// <value>
	/// A list containing all index definitions for the table.
	/// </value>
	/// <remarks>
	/// Indexes improve query performance by providing efficient access paths to table data.
	/// The index collection includes information about which columns are indexed and the
	/// index names, enabling index management and optimization.
	/// </remarks>
	List<ITableIndex> Indexes { get; }
}
