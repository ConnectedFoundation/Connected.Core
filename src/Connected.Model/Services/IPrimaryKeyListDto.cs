namespace Connected.Services;

/// <summary>
/// Represents a data transfer object containing a list of primary keys.
/// </summary>
/// <typeparam name="T">The type of the primary keys in the list.</typeparam>
/// <remarks>
/// This interface provides a standardized way to perform batch operations on multiple
/// entities by specifying their primary keys. This is commonly used for bulk select,
/// update, or delete operations.
/// </remarks>
public interface IPrimaryKeyListDto<T>
	: IDto
{
	/// <summary>
	/// Gets or sets the list of primary key identifiers.
	/// </summary>
	List<T> Items { get; set; }
}
