namespace Connected.Services;

/// <summary>
/// Represents options for paginating query results.
/// </summary>
/// <remarks>
/// This interface defines the page size and index for retrieving a specific subset of
/// query results. Pagination helps manage large result sets by dividing them into
/// manageable chunks.
/// </remarks>
public interface IPagingOptions
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
