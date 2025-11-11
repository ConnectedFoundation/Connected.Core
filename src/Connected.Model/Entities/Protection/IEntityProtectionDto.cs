using Connected.Services;

namespace Connected.Entities.Protection;

/// <summary>
/// DTO carrying an entity instance and its desired persistence <see cref="State"/> for protection operations.
/// </summary>
/// <typeparam name="TEntity">The entity type subject to protection.</typeparam>
/// <remarks>
/// Provides the target <see cref="Entity"/> and the persistence <see cref="State"/> indicating whether the entity should be
/// added, updated or deleted as part of a protection workflow. Implementations are consumed by protection services and protectors.
/// </remarks>
public interface IEntityProtectionDto<TEntity>
	: IDto
	where TEntity : IEntity
{
	/// <summary>
	/// Gets or sets the entity instance to protect.
	/// </summary>
	TEntity Entity { get; set; }
	/// <summary>
	/// Gets or sets the persistence state for the entity (Add, Update, Delete).
	/// </summary>
	State State { get; set; }
}
