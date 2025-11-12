namespace Connected.Storage.Oracle.Schemas;

/// <summary>
/// Represents complete metadata for an Oracle database table.
/// </summary>
/// <remarks>
/// This class encapsulates all structural information about a database table including
/// constraints, indexes, and identity column settings. It aggregates metadata from multiple
/// Oracle data dictionary views (ALL_CONSTRAINTS, ALL_CONS_COLUMNS, ALL_INDEXES, ALL_IND_COLUMNS)
/// to provide a comprehensive view of the table structure. This information is essential for
/// schema comparison and synchronization operations, enabling detection of structural differences
/// between the application's schema definition and the actual database state.
/// </remarks>
internal sealed class ObjectDescriptor
{
	/// <summary>
	/// Gets or sets the collection of constraints defined on the table.
	/// </summary>
	/// <value>
	/// A list of constraint descriptors including primary keys, foreign keys, unique constraints,
	/// check constraints, and default constraints.
	/// </value>
	/// <remarks>
	/// Oracle constraint names must be unique within a schema and are limited to 30 characters
	/// (pre-12.2) or 128 characters (12.2+). Constraints include P (primary key), R (foreign key),
	/// U (unique), C (check), and others as defined in ALL_CONSTRAINTS.
	/// </remarks>
	public List<ObjectConstraint> Constraints { get; set; } = [];

	/// <summary>
	/// Gets or sets the collection of indexes defined on the table.
	/// </summary>
	/// <value>
	/// A list of index descriptors including B-tree, bitmap, function-based, and other index types.
	/// </value>
	/// <remarks>
	/// Oracle indexes can be unique or non-unique, and support various types including B-tree
	/// (default), bitmap, function-based, and domain indexes. Index metadata is retrieved from
	/// ALL_INDEXES and ALL_IND_COLUMNS views.
	/// </remarks>
	public List<ObjectIndex> Indexes { get; set; } = [];

	/// <summary>
	/// Gets or sets the identity column configuration for the table.
	/// </summary>
	/// <value>
	/// The identity descriptor containing sequence information, or <c>null</c> if no identity column exists.
	/// </value>
	/// <remarks>
	/// Oracle 12c+ supports GENERATED AS IDENTITY for automatic identity columns. For earlier versions,
	/// identity behavior is implemented using sequences and triggers. This property captures the identity
	/// configuration regardless of implementation method.
	/// </remarks>
	public ObjectIdentity? Identity { get; set; }
}
