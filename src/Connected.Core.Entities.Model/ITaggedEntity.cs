namespace Connected.Entities;

public interface ITaggedEntity : IEntity
{
	string? Tags { get; init; }
}
