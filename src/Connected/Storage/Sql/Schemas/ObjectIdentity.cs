namespace Connected.Storage.Sql.Schemas;

/// <summary>
/// Represents identity column configuration for a database table.
/// </summary>
/// <remarks>
/// This class encapsulates identity column metadata obtained from SQL Server system catalogs.
/// Identity columns automatically generate sequential numeric values for new rows, typically
/// used for primary key columns. The configuration includes the column name, seed value
/// (starting value), increment value (step between values), and replication behavior. This
/// information is critical during schema synchronization as identity column properties cannot
/// be modified through ALTER TABLE statements and require table recreation when changed.
/// </remarks>
internal class ObjectIdentity
{
	/// <summary>
	/// Gets or sets the name of the identity column.
	/// </summary>
	/// <value>
	/// The column name that has the identity property, or <c>null</c> if the table has no identity column.
	/// </value>
	public string? Identity { get; set; }

	/// <summary>
	/// Gets or sets the seed value for the identity column.
	/// </summary>
	/// <value>
	/// The starting value for the identity sequence.
	/// </value>
	public int Seed { get; set; }

	/// <summary>
	/// Gets or sets the increment value for the identity column.
	/// </summary>
	/// <value>
	/// The value added to the identity value of the previous row to generate the next identity value.
	/// </value>
	public int Increment { get; set; }

	/// <summary>
	/// Gets or sets a value indicating whether the identity column is excluded from replication.
	/// </summary>
	/// <value>
	/// <c>true</c> if identity values are not replicated; otherwise, <c>false</c>.
	/// </value>
	public bool NotForReplication { get; set; }
}
