namespace Connected.Storage.Schemas;

/// <summary>
/// Defines a middleware component for schema synchronization operations.
/// </summary>
/// <remarks>
/// This interface represents specialized middleware that participates in schema
/// synchronization processes. Schema middleware can perform custom validation, transformation,
/// or processing logic during schema updates and migrations. Multiple middleware components
/// can be registered to form a pipeline that processes schemas before they are applied to
/// the database. As middleware components, schema middleware participates in the application's
/// lifecycle and can be dynamically discovered and initialized. This pattern enables
/// extensible schema management where custom logic can be injected into the schema
/// synchronization workflow.
/// </remarks>
public interface ISchemaMiddleware
	: IMiddleware
{
	/// <summary>
	/// Asynchronously invokes the middleware to process a schema operation.
	/// </summary>
	/// <param name="dto">The schema middleware parameters containing the type and schema definition.</param>
	/// <returns>
	/// A task that represents the asynchronous operation. The task result indicates whether
	/// the schema operation should continue (<c>true</c>) or be aborted (<c>false</c>).
	/// </returns>
	/// <remarks>
	/// This method is called during schema synchronization operations, allowing the middleware
	/// to inspect, validate, or modify schema definitions before they are applied. Middleware
	/// can return false to prevent a schema from being processed, enabling validation rules
	/// or conditional schema application. Multiple middleware components are invoked in sequence,
	/// with processing stopping if any middleware returns false.
	/// </remarks>
	Task<bool> Invoke(ISchemaMiddlewareDto dto);
}
