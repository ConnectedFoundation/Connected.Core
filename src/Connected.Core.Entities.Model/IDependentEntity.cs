namespace Connected.Entities;

public interface IDependentEntity<THead, TPrimaryKey> : IEntity, IPrimaryKeyEntity<TPrimaryKey>
	where THead : notnull
	where TPrimaryKey : notnull
{
	THead Head { get; init; }
}
