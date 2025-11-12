namespace Connected.Storage.Schemas;

/// <summary>
/// Represents a foreign key referential constraint in a database schema.
/// </summary>
/// <remarks>
/// This interface defines the metadata for referential integrity constraints (foreign keys)
/// that enforce relationships between tables in a database. Referential constraints ensure
/// data consistency by maintaining valid references between related tables. The constraint
/// includes information about the referenced table, match options, and cascade rules for
/// update and delete operations. This metadata is essential for schema synchronization,
/// validation, and understanding the relational structure of the database.
/// </remarks>
public interface IReferentialConstraint
{
	/// <summary>
	/// Gets the name of the referential constraint.
	/// </summary>
	/// <value>
	/// A string representing the constraint name.
	/// </value>
	/// <remarks>
	/// The constraint name uniquely identifies the foreign key within the database schema,
	/// typically following database-specific naming conventions.
	/// </remarks>
	string Name { get; }

	/// <summary>
	/// Gets the schema name of the referenced table.
	/// </summary>
	/// <value>
	/// A string representing the schema name where the referenced table resides.
	/// </value>
	/// <remarks>
	/// The reference schema specifies which schema contains the table that this foreign
	/// key references, enabling cross-schema relationships.
	/// </remarks>
	string ReferenceSchema { get; }

	/// <summary>
	/// Gets the name of the referenced table.
	/// </summary>
	/// <value>
	/// A string representing the name of the table being referenced.
	/// </value>
	/// <remarks>
	/// The reference name identifies the target table that this foreign key points to,
	/// establishing the relationship between the two tables.
	/// </remarks>
	string ReferenceName { get; }

	/// <summary>
	/// Gets the match option for the referential constraint.
	/// </summary>
	/// <value>
	/// A string representing the match option (e.g., "SIMPLE", "FULL", "PARTIAL").
	/// </value>
	/// <remarks>
	/// The match option determines how composite foreign keys are validated against
	/// the referenced table's primary or unique key.
	/// </remarks>
	string MatchOption { get; }

	/// <summary>
	/// Gets the action to perform when the referenced row is updated.
	/// </summary>
	/// <value>
	/// A string representing the update rule (e.g., "CASCADE", "NO ACTION", "SET NULL", "SET DEFAULT").
	/// </value>
	/// <remarks>
	/// The update rule specifies what happens to rows with foreign key values when the
	/// corresponding primary key value in the referenced table is updated.
	/// </remarks>
	string UpdateRule { get; }

	/// <summary>
	/// Gets the action to perform when the referenced row is deleted.
	/// </summary>
	/// <value>
	/// A string representing the delete rule (e.g., "CASCADE", "NO ACTION", "SET NULL", "SET DEFAULT").
	/// </value>
	/// <remarks>
	/// The delete rule specifies what happens to rows with foreign key values when the
	/// corresponding primary key row in the referenced table is deleted.
	/// </remarks>
	string DeleteRule { get; }
}
