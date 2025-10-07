namespace Connected.Entities;

public interface IPrimaryKeyEntity<T> : IEntity
	where T : notnull
{
	T Id { get; init; }
}
