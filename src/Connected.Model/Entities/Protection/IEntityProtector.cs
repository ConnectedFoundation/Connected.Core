namespace Connected.Entities.Protection;
/// <summary>
/// Middleware component that applies protection logic to an entity prior to persistence.
/// </summary>
/// <typeparam name="TEntity">The protected entity type.</typeparam>
/// <remarks>
/// Protectors are resolved and invoked by the protection service to enforce invariants or authorization
/// rules depending on the entity's desired persistence <see cref="State"/>.
/// </remarks>
public interface IEntityProtector<TEntity>
	: IMiddleware
	where TEntity : IEntity
{
	/// <summary>
	/// Executes protection logic for the provided entity protection DTO.
	/// </summary>
	/// <param name="dto">Protection DTO containing entity and requested state.</param>
	Task Invoke(IEntityProtectionDto<TEntity> dto);
}
