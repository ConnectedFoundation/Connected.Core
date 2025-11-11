namespace Connected.Services;

/// <summary>
/// Represents a data transfer object containing a single value.
/// </summary>
/// <typeparam name="TPrimaryKey">The type of the value.</typeparam>
/// <remarks>
/// This interface provides a standardized way to transfer a single value, commonly
/// used for simple query parameters or selection criteria based on a specific value.
/// </remarks>
public interface IValueDto<TPrimaryKey>
	: IDto
{
	/// <summary>
	/// Gets or sets the value.
	/// </summary>
	TPrimaryKey Value { get; set; }
}
