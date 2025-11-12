using Connected.Services;

namespace Connected.Storage.Schemas;

/// <summary>
/// Represents a data transfer object for selecting a schema definition.
/// </summary>
/// <remarks>
/// This interface defines the parameters required to retrieve a schema definition from
/// the schema service. By specifying an entity type, the schema service can look up and
/// return the corresponding database schema structure including tables, columns, indexes,
/// and constraints. This DTO is used when applications need to inspect existing schema
/// definitions for validation, comparison, or documentation purposes.
/// </remarks>
public interface ISelectSchemaDto
	: IDto
{
	/// <summary>
	/// Gets or sets the entity type for which to retrieve the schema.
	/// </summary>
	/// <value>
	/// A <see cref="Type"/> representing the entity class.
	/// </value>
	/// <remarks>
	/// The type identifies which entity model's schema should be retrieved. The schema
	/// service uses this type to locate the corresponding database schema definition,
	/// including all structural metadata.
	/// </remarks>
	Type Type { get; set; }
}