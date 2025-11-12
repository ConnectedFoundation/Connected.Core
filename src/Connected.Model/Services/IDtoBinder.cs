namespace Connected.Services;

/// <summary>
/// Provides functionality to bind values to DTO instances.
/// </summary>
/// <remarks>
/// DTO binders enable dynamic property binding from various sources to DTO objects.
/// This mechanism is typically used during DTO hydration or when mapping values from
/// external sources such as HTTP requests, message queues, or other data providers.
/// </remarks>
public interface IDtoBinder
{
	/// <summary>
	/// Binds values to the specified instance using the provided arguments.
	/// </summary>
	/// <param name="instance">The target instance to which values will be bound.</param>
	/// <param name="arguments">The source arguments containing the values to bind.</param>
	void Invoke(object instance, params object[] arguments);
}