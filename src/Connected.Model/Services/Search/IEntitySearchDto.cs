namespace Connected.Services.Search;

/// <summary>
/// Represents a data transfer object for searching entities with entity-specific context.
/// </summary>
/// <remarks>
/// This interface extends the base search DTO with entity type and identifier properties,
/// enabling search operations that are scoped to specific entity contexts. This is useful
/// for searching within related entities or filtering search results based on entity ownership.
/// </remarks>
public interface IEntitySearchDto
	: ISearchDto
{
	/// <summary>
	/// Gets or sets the entity type name.
	/// </summary>
	string Entity { get; set; }

	/// <summary>
	/// Gets or sets the entity identifier.
	/// </summary>
	string EntityId { get; set; }
}
