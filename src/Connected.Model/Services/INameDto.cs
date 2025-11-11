namespace Connected.Services;

/// <summary>
/// Represents a data transfer object containing a name value.
/// </summary>
/// <remarks>
/// This interface provides a standardized way to transfer name-based queries or filters,
/// commonly used for searching or selecting entities by their name property.
/// </remarks>
public interface INameDto
	: IDto
{
	/// <summary>
	/// Gets or sets the name value.
	/// </summary>
	string? Name { get; set; }
}
