namespace Connected.Storage.Sql.Schemas;

/// <summary>
/// Represents filegroup information for a database table.
/// </summary>
/// <remarks>
/// This class encapsulates filegroup metadata obtained from SQL Server system catalogs.
/// Filegroups are logical storage units in SQL Server that allow administrators to control
/// the physical placement of tables and indexes on disk. This information is used during
/// schema synchronization to ensure tables are created in the appropriate filegroup,
/// maintaining consistency with the existing database structure or configuration requirements.
/// </remarks>
internal sealed class ObjectFileGroup
{
	/// <summary>
	/// Gets or sets the filegroup name where the table is stored.
	/// </summary>
	/// <value>
	/// The name of the filegroup, or <c>null</c> if stored in the default filegroup.
	/// </value>
	public string? FileGroup { get; set; }
}
