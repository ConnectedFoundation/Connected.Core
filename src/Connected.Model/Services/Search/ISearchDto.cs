namespace Connected.Services.Search;

/// <summary>
/// Represents a data transfer object for text-based search operations with pagination.
/// </summary>
/// <remarks>
/// This interface provides the foundational contract for search operations, combining
/// text-based search criteria with pagination capabilities. It serves as the base for
/// more specialized search DTOs that add additional filtering or context.
/// </remarks>
public interface ISearchDto
	: IDto
{
	/// <summary>
	/// Gets or sets the pagination settings for the search results.
	/// </summary>
	IPaging Paging { get; set; }

	/// <summary>
	/// Gets or sets the search text used for filtering results.
	/// </summary>
	string Text { get; set; }
}
