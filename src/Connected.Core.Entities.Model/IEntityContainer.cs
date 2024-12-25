namespace Connected.Entities;

public interface IEntityContainer<TPrimaryKey> : IPrimaryKeyEntity<TPrimaryKey>
	where TPrimaryKey : notnull
{
	string Entity { get; init; }
	string EntityId { get; init; }
}
