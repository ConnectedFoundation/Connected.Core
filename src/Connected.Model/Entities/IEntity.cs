using System.Text.Json.Serialization;

namespace Connected.Entities;

public enum State : byte
{
	Add = 0,
	Update = 1,
	Delete = 2
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
