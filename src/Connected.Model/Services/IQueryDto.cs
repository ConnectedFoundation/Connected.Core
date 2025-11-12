namespace Connected.Services;

/// <summary>
/// Represents a data transfer object for querying entities with sorting and pagination.
/// </summary>
/// <remarks>
/// This interface provides standard query capabilities including ordering by multiple
/// properties and paginating results. It serves as the base for more specialized query
/// DTOs that add additional filtering criteria.
/// </remarks>
public interface IQueryDto
	: IDto
{
	/// <summary>
	/// Gets or sets the list of sorting descriptors for ordering query results.
	/// </summary>
	List<IOrderByDescriptor> OrderBy { get; set; }

	/// <summary>
	/// Gets or sets the pagination options for limiting query results.
	/// </summary>
	IPagingOptions Paging { get; set; }
}
