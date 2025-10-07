namespace Connected.Entities;

public interface IEntityPropertySerializer
{
	Task<object?> Serialize(IEntity entity, object? value, CancellationToken cancel = default);
	Task<object?> Deserialize(IEntity entity, object? value, CancellationToken cancel = default);
}