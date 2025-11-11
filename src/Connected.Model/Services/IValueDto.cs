namespace Connected.Services;

/// <summary>
/// Represents a data transfer object containing a list of values.
/// </summary>
/// <typeparam name="TPrimaryKey">The type of the values in the list.</typeparam>
/// <remarks>
/// This interface provides a standardized way to transfer multiple values, commonly
/// used for batch operations, filtering by multiple criteria, or selection of multiple
/// items by their values.
/// </remarks>
public interface IValueListDto<TPrimaryKey>
	: IDto
{
	/// <summary>
	/// Gets or sets the list of values.
	/// </summary>
	List<TPrimaryKey> Items { get; set; }
}
