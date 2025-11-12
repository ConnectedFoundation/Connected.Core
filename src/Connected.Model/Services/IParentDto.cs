namespace Connected.Services;

/// <summary>
/// Represents a data transfer object containing a parent reference.
/// </summary>
/// <typeparam name="T">The type of the parent identifier.</typeparam>
/// <remarks>
/// This interface provides a standardized way to represent hierarchical relationships
/// by storing a reference to a parent entity. This is commonly used for tree structures
/// or nested data where entities have parent-child relationships.
/// </remarks>
public interface IParentDto<T>
	: IDto
{
	/// <summary>
	/// Gets or sets the parent identifier.
	/// </summary>
	T? Parent { get; set; }
}
