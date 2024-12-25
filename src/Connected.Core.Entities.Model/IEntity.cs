using System.Text.Json.Serialization;

namespace Connected.Entities;

public enum State : byte
{
	Default = 0,
	New = 1,
	Deleted = 2
}

public interface IEntity
{
	[JsonIgnore]
	State State { get; init; }
}

public interface IEntity<T> : IEntity, IPrimaryKeyEntity<T>
	where T : notnull
{
}
