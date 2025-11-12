namespace Connected.Storage.Schemas;

/// <summary>
/// Represents a table constraint in the database schema.
/// </summary>
/// <remarks>
/// This interface extends the base schema definition to represent constraints applied to
/// database tables such as check constraints, unique constraints, and other validation rules.
/// Table constraints enforce data integrity by restricting the values that can be stored in
/// tables beyond simple column-level constraints. They can span multiple columns and implement
/// complex validation logic using SQL expressions. This interface provides the base structure
/// for all constraint types, inheriting common schema properties like name, schema, and type.
/// </remarks>
public interface ITableConstraint
	: ISchema
{
}
