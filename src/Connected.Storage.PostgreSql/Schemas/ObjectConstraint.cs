using Connected.Storage.Schemas;

namespace Connected.Storage.PostgreSql.Schemas;

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
/// Describes a database constraint associated with a table.
/// </summary>
/// <remarks>
/// This record encapsulates constraint metadata including its name, type, and associated columns.
/// It is used during schema synchronization to identify existing constraints that need to be
/// preserved, modified, or dropped when altering table structure. The descriptor supports
/// various constraint types including primary keys, unique constraints, defaults, checks,
/// and foreign keys.
/// </remarks>
internal sealed record ObjectConstraint
{
	/// <summary>
	/// Gets or sets the constraint name.
	/// </summary>
	public string? Name { get; init; }

	/// <summary>
	/// Gets or sets the constraint type.
	/// </summary>
	public ConstraintType ConstraintType { get; init; }

	/// <summary>
	/// Gets or sets the columns involved in the constraint.
	/// </summary>
	/// <remarks>
	/// For composite constraints, this list contains multiple columns in the order
	/// they appear in the constraint definition.
	/// </remarks>
	public List<ISchemaColumn> Columns { get; init; } = [];
}
