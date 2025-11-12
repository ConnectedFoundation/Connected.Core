namespace Connected.Storage.Schemas;

/// <summary>
/// Represents a database index definition.
/// </summary>
/// <remarks>
/// This interface defines the structure of a database index, which improves query performance
/// by providing efficient access paths to table data. Indexes can span multiple columns,
/// creating composite indexes that optimize queries involving multiple criteria. The interface
/// specifies which columns participate in the index and the index name for management purposes.
/// Indexes are essential for query optimization but come with trade-offs in terms of storage
/// space and write performance. This metadata is used during schema synchronization to ensure
/// that database indexes match the expected index definitions from entity models.
/// </remarks>
public interface ITableIndex
{
	/// <summary>
	/// Gets the name of the index.
	/// </summary>
	/// <value>
	/// A string representing the index name.
	/// </value>
	/// <remarks>
	/// The index name uniquely identifies the index within the table, enabling index
	/// management operations such as creation, modification, or deletion.
	/// </remarks>
	string Name { get; }

	/// <summary>
	/// Gets the collection of columns included in the index.
	/// </summary>
	/// <value>
	/// A list of column names that participate in the index.
	/// </value>
	/// <remarks>
	/// The columns list defines which table columns are indexed and their order within
	/// the index. The order is significant for composite indexes, as it affects query
	/// optimization and index usage. The first column in the list is the primary sort
	/// column, with subsequent columns providing secondary sorting.
	/// </remarks>
	List<string> Columns { get; }
}
