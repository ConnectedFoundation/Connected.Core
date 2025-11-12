using Connected.Storage.Schemas;

namespace Connected.Storage.Oracle.Schemas;

/// <summary>
/// Specifies the type of database constraint.
/// </summary>
internal enum ConstraintType
{
	/// <summary>
	/// Primary key constraint.
	/// </summary>
	PrimaryKey = 1,

	/// <summary>
	/// Unique constraint.
	/// </summary>
	Unique = 2,

	/// <summary>
	/// Default value constraint.
	/// </summary>
	Default = 3,

	/// <summary>
	/// Check constraint.
	/// </summary>
	Check = 4,

	/// <summary>
	/// Foreign key constraint.
	/// </summary>
	ForeignKey = 5
}

/// <summary>
/// Represents a database constraint defined on an Oracle table.
/// </summary>
/// <remarks>
/// This class encapsulates metadata about constraints including primary keys, foreign keys,
/// unique constraints, check constraints, and default constraints. Oracle stores constraint
/// information in ALL_CONSTRAINTS and ALL_CONS_COLUMNS system views. Constraint names are
/// limited to 30 characters (pre-12.2) or 128 characters (12.2+) and must be unique within
/// a schema. The constraint type is indicated by CONSTRAINT_TYPE column: P (primary key),
/// R (foreign key), U (unique), C (check/not null).
/// </remarks>
internal sealed class ObjectConstraint
{
	/// <summary>
	/// Gets or sets the constraint name.
	/// </summary>
	/// <value>
	/// The unique constraint identifier within the schema, or <c>null</c> for system-generated names.
	/// </value>
	/// <remarks>
	/// Oracle constraint names are case-insensitive and stored in uppercase unless quoted during
	/// creation. System-generated constraint names typically follow the pattern SYS_Cnnnnn.
	/// </remarks>
	public string? Name { get; set; }

	/// <summary>
	/// Gets or sets the type of constraint.
	/// </summary>
	/// <value>
	/// The constraint type enumeration value indicating the constraint category.
	/// </value>
	/// <remarks>
	/// Oracle constraint types map to ConstraintType enumeration:
	/// - P (Primary Key) ? ConstraintType.PrimaryKey
	/// - R (Foreign Key) ? ConstraintType.ForeignKey
	/// - U (Unique) ? ConstraintType.Unique
	/// - C (Check) ? ConstraintType.Check
	/// NOT NULL constraints are stored as check constraints in Oracle.
	/// </remarks>
	public ConstraintType ConstraintType { get; set; }

	/// <summary>
	/// Gets or sets the columns involved in the constraint.
	/// </summary>
	/// <value>
	/// A list of column names that participate in the constraint definition.
	/// </value>
	/// <remarks>
	/// For primary keys, unique constraints, and foreign keys, multiple columns may be involved
	/// in a composite constraint. Column order is preserved as defined in ALL_CONS_COLUMNS.POSITION.
	/// For check constraints, this list may be empty as the constraint is defined by a search
	/// condition rather than specific columns.
	/// </remarks>
	public List<string> Columns { get; set; } = [];

	/// <summary>
	/// Gets or sets the referenced table name for foreign key constraints.
	/// </summary>
	/// <value>
	/// The name of the table referenced by a foreign key constraint, or <c>null</c> for non-FK constraints.
	/// </value>
	/// <remarks>
	/// This property is only populated for foreign key constraints (ConstraintType.ForeignKey).
	/// Oracle stores this information in the R_OWNER and R_CONSTRAINT_NAME columns of ALL_CONSTRAINTS,
	/// which can be joined to resolve the referenced table name.
	/// </remarks>
	public string? ReferenceTable { get; set; }

	/// <summary>
	/// Gets or sets the referenced columns for foreign key constraints.
	/// </summary>
	/// <value>
	/// A list of column names in the referenced table, or empty for non-FK constraints.
	/// </value>
	/// <remarks>
	/// This property is only populated for foreign key constraints and must align with the Columns
	/// property in terms of count and order. Oracle enforces referential integrity between these
	/// column pairs.
	/// </remarks>
	public List<string> ReferenceColumns { get; set; } = [];
}
