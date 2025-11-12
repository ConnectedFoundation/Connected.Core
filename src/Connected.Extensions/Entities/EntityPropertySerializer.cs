using System.Threading;
using System.Threading.Tasks;

namespace Connected.Entities;

public abstract class EntityPropertySerializer : IEntityPropertySerializer
{
	protected CancellationToken Cancel { get; private set; }
	protected IEntity Entity { get; private set; } = default!;

	public Task<object?> Serialize(IEntity entity, object? value, CancellationToken cancel = default)
	{
		Entity = entity;
		Cancel = cancel;

		return OnSerialize(value);
	}

	protected virtual Task<object?> OnSerialize(object? value)
	{
		return Task.FromResult(value);
	}

	public Task<object?> Deserialize(IEntity entity, object? value, CancellationToken cancel = default)
	{
		Entity = entity;
		Cancel = cancel;

		return OnDeserialize(value);
	}

	protected virtual Task<object?> OnDeserialize(object? value)
	{
		return Task.FromResult(value);
	}
}