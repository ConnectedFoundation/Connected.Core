using Connected.Annotations;

namespace Connected.Entities.Protection;

/// <summary>
/// Service orchestrating entity protection by delegating to configured protectors.
/// </summary>
/// <remarks>
/// Accepts a protection DTO containing the target entity and desired state, then invokes appropriate
/// protectors to enforce business rules or apply safeguards prior to persistence.
/// </remarks>
[Service]
public interface IEntityProtectionService
{
	/// <summary>
	/// Invokes entity protection for the specified entity type and state.
	/// </summary>
	/// <typeparam name="TEntity">The entity type.</typeparam>
	/// <param name="dto">Protection DTO carrying entity and state.</param>
	Task Invoke<TEntity>(IEntityProtectionDto<TEntity> dto)
		where TEntity : IEntity;
}
