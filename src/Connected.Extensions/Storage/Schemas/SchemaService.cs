using Connected.Services;
using Connected.Storage.Schemas.Ops;

namespace Connected.Storage.Schemas;

/// <summary>
/// Provides schema management operations for entity types.
/// </summary>
/// <remarks>
/// This service acts as the entry point for schema-related operations including schema selection
/// and synchronization. It delegates to specialized operation classes to perform the actual work,
/// following the service-operation pattern common throughout the framework. The service supports
/// retrieving schema definitions from entity types and updating database schemas to match entity
/// model changes. This is a critical component in the storage layer's schema management infrastructure,
/// ensuring that database structure remains synchronized with the application's entity model.
/// </remarks>
internal class SchemaService(IServiceProvider services)
	: Service(services), ISchemaService
{
	/// <summary>
	/// Updates database schemas for the specified entity types.
	/// </summary>
	/// <param name="dto">The DTO containing the entity types to synchronize.</param>
	/// <returns>A task representing the asynchronous update operation.</returns>
	/// <remarks>
	/// This method initiates the schema synchronization process by invoking the Update operation.
	/// It processes all entity types specified in the DTO and ensures their database schemas are
	/// synchronized through registered middleware components. The operation handles both new schema
	/// creation and modification of existing schemas based on entity model changes.
	/// </remarks>
	public async Task Update(IUpdateSchemaDto dto)
	{
		await Invoke(GetOperation<Update>(), dto);
	}

	/// <summary>
	/// Retrieves the schema definition for an entity type.
	/// </summary>
	/// <param name="dto">The DTO containing the entity type to retrieve schema for.</param>
	/// <returns>
	/// A task representing the asynchronous operation. The task result contains the schema
	/// definition if found; otherwise, <c>null</c>.
	/// </returns>
	/// <remarks>
	/// This method analyzes the specified entity type's metadata through reflection to build
	/// a complete schema representation including table information, columns, constraints, and
	/// type mappings. The resulting schema can be used for validation, comparison, or DDL generation
	/// operations.
	/// </remarks>
	public async Task<ISchema?> Select(ISelectSchemaDto dto)
	{
		return await Invoke(GetOperation<Select>(), dto);
	}
}
