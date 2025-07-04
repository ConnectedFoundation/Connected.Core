namespace Connected.Entities;

public interface ITaggedEntity<TPrimaryKey> : IEntity<TPrimaryKey>
	where TPrimaryKey : notnull
{
	string? Tags { get; init; }
}
