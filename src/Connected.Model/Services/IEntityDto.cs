namespace Connected.Services;

/// <summary>
/// Represents a data transfer object that references an entity.
/// </summary>
/// <remarks>
/// This interface provides a generic way to reference entities by storing both the entity
/// type name and its identifier as strings. This enables loosely-coupled entity references
/// across different modules or in scenarios where strong typing is not feasible.
/// </remarks>
public interface IEntityDto
	: IDto
{
	/// <summary>
	/// Gets or sets the entity type name.
	/// </summary>
	string Entity { get; set; }

	/// <summary>
	/// Gets or sets the entity identifier as a string.
	/// </summary>
	string EntityId { get; set; }
}
