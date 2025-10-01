using Connected.Entities;

namespace Connected.Core.Entities.Mock;

public class EntityMock : IEntity
{
	public State State { get; init; }
}

public class EntityMock<TPrimaryKey> : EntityMock, IEntity<TPrimaryKey>
	where TPrimaryKey : notnull
{
	public required TPrimaryKey Id { get; init; }
}
