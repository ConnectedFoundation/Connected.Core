using Connected.Annotations;

namespace Connected.Storage.Schemas;

/// <summary>
/// Provides services for managing database schemas.
/// </summary>
/// <remarks>
/// This service implements schema management functionality including schema synchronization,
/// retrieval, and validation. It enables applications to ensure that database structures
/// match entity model definitions by comparing existing schemas with expected schemas and
/// performing necessary updates. The service supports both reading current schema information
/// and updating schemas to match entity models. This is essential for maintaining database
/// consistency across deployments, handling migrations, and ensuring that the database
/// structure supports the application's data access requirements.
/// </remarks>
[Service]
public interface ISchemaService
{
	/// <summary>
	/// Asynchronously updates database schemas to match entity model definitions.
	/// </summary>
	/// <param name="dto">The update parameters containing the list of entity types to synchronize.</param>
	/// <returns>A task that represents the asynchronous update operation.</returns>
	/// <remarks>
	/// This method performs schema synchronization for the specified entity types, comparing
	/// the current database schema with the expected schema derived from entity models.
	/// It applies necessary changes such as creating tables, adding columns, creating indexes,
	/// or modifying constraints to ensure the database structure matches the entity definitions.
	/// The operation may invoke schema middleware components to enable custom validation or
	/// transformation logic during the synchronization process.
	/// </remarks>
	Task Update(IUpdateSchemaDto dto);

	/// <summary>
	/// Asynchronously retrieves the schema definition for a specific entity type.
	/// </summary>
	/// <param name="dto">The selection parameters containing the entity type.</param>
	/// <returns>
	/// A task that represents the asynchronous operation. The task result contains
	/// the schema definition if found, or null if the schema does not exist.
	/// </returns>
	/// <remarks>
	/// This method retrieves the database schema information for the specified entity type,
	/// including table structure, columns, indexes, and constraints. The returned schema
	/// can be used for validation, comparison, or inspection purposes. If the entity type
	/// has not been synchronized to the database or does not have a corresponding schema,
	/// null is returned.
	/// </remarks>
	Task<ISchema?> Select(ISelectSchemaDto dto);
}
