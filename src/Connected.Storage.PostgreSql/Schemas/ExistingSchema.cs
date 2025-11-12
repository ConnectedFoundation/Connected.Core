namespace Connected.Storage.PostgreSql.Schemas;

/// <summary>
/// Represents the existing schema structure loaded from a PostgreSQL database.
/// </summary>
/// <remarks>
/// This record encapsulates the metadata of an existing database table including its
/// descriptor containing columns, constraints, and indexes. It is used during schema
/// synchronization to compare the current database state with the desired schema definition
/// and determine necessary modifications. The nullable descriptor indicates whether the
/// table exists in the database.
/// </remarks>
internal sealed record ExistingSchema
{
	/// <summary>
	/// Gets or sets the table descriptor containing column and constraint information.
	/// </summary>
	/// <value>
	/// The <see cref="ObjectDescriptor"/> with table metadata, or <c>null</c> if the table doesn't exist.
	/// </value>
	public ObjectDescriptor? Descriptor { get; init; }
}
