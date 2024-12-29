using Connected.Annotations;
using Connected.Entities.Protection;
using Connected.Services;

namespace Connected.Entities;
internal sealed class EntityProtectionDto<TEntity> : Dto, IEntityProtectionDto<TEntity>
	where TEntity : IEntity
{
	[NonDefault, SkipValidation]
	public TEntity Entity { get; set; } = default!;
	public State State { get; set; }
}
