namespace Connected.Entities;

public interface IConsistentEntity<T> : IEntity<T>
	 where T : notnull
{
	string? ETag { get; }
}
