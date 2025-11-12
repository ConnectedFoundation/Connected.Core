namespace Connected.Services;

/// <summary>
/// Represents a data transfer object containing a list of tag strings.
/// </summary>
/// <remarks>
/// This interface provides a standardized way to transfer multiple tag values, commonly
/// used for filtering or categorizing entities by tags. Tags enable flexible classification
/// and search capabilities.
/// </remarks>
public interface ITagListDto
	: IDto
{
	/// <summary>
	/// Gets or sets the list of tag strings.
	/// </summary>
	List<string> Items { get; set; }
}
