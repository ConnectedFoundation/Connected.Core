namespace Connected.Services;

/// <summary>
/// Represents a data transfer object containing a list of head values.
/// </summary>
/// <typeparam name="T">The type of the head values in the list.</typeparam>
/// <remarks>
/// This interface provides a standardized way to transfer multiple head values, typically
/// used for batch operations across multiple partitions, tenants, or organizational units
/// in multi-tenant or distributed scenarios.
/// </remarks>
public interface IHeadListDto<T>
	: IDto
{
	/// <summary>
	/// Gets or sets the list of head values.
	/// </summary>
	List<T> Items { get; set; }
}
