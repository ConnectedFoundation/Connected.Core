namespace Connected.Services.Search;

/// <summary>
/// Represents pagination settings for search operations.
/// </summary>
/// <remarks>
/// This interface defines the page size and index for paginating search results,
/// allowing retrieval of result subsets in manageable chunks. This is specifically
/// designed for search scenarios where result sets may be large.
/// </remarks>
public interface IPaging
{
	/// <summary>
	/// Gets or sets the number of items per page.
	/// </summary>
	int Size { get; set; }

	/// <summary>
	/// Gets or sets the zero-based page index.
	/// </summary>
	int Index { get; set; }
}
