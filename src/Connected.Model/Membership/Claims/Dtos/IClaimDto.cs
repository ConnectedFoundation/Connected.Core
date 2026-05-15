using Connected.Services;

namespace Connected.Membership.Claims.Dtos;

public interface IClaimDto : IDto
{
	/// <summary>
	/// The claim Key
	/// </summary>
	string Value { get; set; }
	/// <summary>
	/// The claim schema, "role" or "user"
	/// </summary>
	string? Schema { get; set; }
	/// <summary>
	/// The token of the schema entity
	/// </summary>
	string? Identity { get; set; }
	/// <summary>
	/// If a claim protects a specific entity, the entity type. Use IClaim.UndefinedEntity for system-wide claims. Equals "*".
	/// </summary>
	string Entity { get; set; }
	/// <summary>
	/// If a claim protects a specific entity, the entity id. Use "*" for all entities of the specified type, or if the entity is unspecified. IClaim.UndefinedId equals this value.
	/// </summary>
	string EntityId { get; set; }
}
