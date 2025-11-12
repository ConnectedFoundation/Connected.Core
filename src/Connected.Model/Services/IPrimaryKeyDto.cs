namespace Connected.Services;

/// <summary>
/// Represents a data transfer object containing a primary key identifier.
/// </summary>
/// <typeparam name="T">The type of the primary key.</typeparam>
/// <remarks>
/// This interface provides a standardized way to reference entities by their primary key,
/// commonly used for select, update, and delete operations that target a specific entity.
/// </remarks>
public interface IPrimaryKeyDto<T>
	: IDto
{
	/// <summary>
	/// Gets or sets the primary key identifier.
	/// </summary>
	public T Id { get; set; }
}
