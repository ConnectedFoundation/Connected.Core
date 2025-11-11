namespace Connected.Services;

/// <summary>
/// Defines the sorting direction for query results.
/// </summary>
public enum OrderByMode
{
	/// <summary>
	/// Sort in ascending order (A to Z, 0 to 9).
	/// </summary>
	Ascending = 0,

	/// <summary>
	/// Sort in descending order (Z to A, 9 to 0).
	/// </summary>
	Descending = 1
}

/// <summary>
/// Represents a descriptor for ordering query results.
/// </summary>
/// <remarks>
/// This interface defines how query results should be sorted by specifying a property
/// name and sorting direction. Multiple order-by descriptors can be combined to create
/// complex sorting logic.
/// </remarks>
public interface IOrderByDescriptor
{
	/// <summary>
	/// Gets or sets the name of the property to sort by.
	/// </summary>
	string Property { get; set; }

	/// <summary>
	/// Gets or sets the sorting direction.
	/// </summary>
	OrderByMode Mode { get; set; }
}
