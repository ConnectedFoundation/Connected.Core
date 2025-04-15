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

public abstract record Entity<T> : Entity, IEntity<T>
	where T : notnull
{
	protected Entity()
	{
		Id = default!;
	}

	protected Entity(T id)
	{
		Id = id;
	}

	[PrimaryKey(true), CacheKey, ReturnValue, Ordinal(-10000)]
	public virtual T Id { get; init; }
}
