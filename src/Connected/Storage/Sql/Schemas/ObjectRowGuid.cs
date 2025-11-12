namespace Connected.Storage.Sql.Schemas;

/// <summary>
/// Represents ROWGUIDCOL information for a database table.
/// </summary>
/// <remarks>
/// This class encapsulates ROWGUIDCOL metadata obtained from SQL Server system catalogs.
/// The ROWGUIDCOL property identifies a uniqueidentifier column that uniquely identifies
/// rows in the table, commonly used for merge replication and other distributed data scenarios.
/// Only one column per table can have the ROWGUIDCOL property. This metadata is used during
/// schema discovery to understand which column, if any, serves as the global unique identifier
/// for the table.
/// </remarks>
internal class ObjectRowGuid
{
	/// <summary>
	/// Gets or sets the name of the ROWGUIDCOL column.
	/// </summary>
	/// <value>
	/// The column name that has the ROWGUIDCOL property, or <c>null</c> if no column has this property.
	/// </value>
	public string? RowGuidCol { get; set; }
}
