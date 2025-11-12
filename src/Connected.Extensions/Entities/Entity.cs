using Connected.Annotations;
using Connected.Annotations.Entities;
using System.ComponentModel;
using System.Text.Json.Serialization;

namespace Connected.Entities;

public abstract record Entity : IEntity
{
	[DefaultValue(State.Update), JsonIgnore, Persistence(PersistenceMode.InMemory)]
	public State State { get; init; } = State.Update;
}

public abstract record Entity<TPrimaryKey> : Entity, IEntity<TPrimaryKey>
	where TPrimaryKey : notnull
{
	protected Entity()
	{
		Id = default!;
	}

	[PrimaryKey(true), CacheKey, ReturnValue, Ordinal(-10000)]
	public virtual TPrimaryKey Id { get; init; } = default!;
}
