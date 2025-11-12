using Connected.Services;

namespace Connected.Storage.Schemas;

/// <summary>
/// Represents a data transfer object for schema middleware operations.
/// </summary>
/// <remarks>
/// This interface encapsulates the parameters passed to schema middleware during schema
/// synchronization operations. It includes both the entity type being processed and the
/// corresponding schema definition. Schema middleware uses this information to perform
/// validation, transformation, or decision logic about whether a schema should be applied.
/// The DTO provides the necessary context for middleware to make informed decisions about
/// schema processing based on both the entity model and the generated schema structure.
/// </remarks>
public interface ISchemaMiddlewareDto
	: IDto
{
	/// <summary>
	/// Gets or sets the entity type being processed.
	/// </summary>
	/// <value>
	/// A <see cref="Type"/> representing the entity class.
	/// </value>
	/// <remarks>
	/// The type identifies which entity model is being synchronized, enabling middleware
	/// to apply type-specific processing logic or validation rules.
	/// </remarks>
	Type Type { get; set; }

	/// <summary>
	/// Gets or sets the schema definition for the entity.
	/// </summary>
	/// <value>
	/// An <see cref="ISchema"/> representing the database schema structure.
	/// </value>
	/// <remarks>
	/// The schema provides the complete database structure definition including columns,
	/// constraints, and indexes. Middleware can inspect or modify this schema before it
	/// is applied to the database.
	/// </remarks>
	ISchema Schema { get; set; }
}