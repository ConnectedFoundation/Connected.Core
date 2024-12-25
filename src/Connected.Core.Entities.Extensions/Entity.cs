using Connected.Annotations;
using Connected.Annotations.Entities;
using System.ComponentModel;
using System.Text.Json.Serialization;

namespace Connected.Entities;

public abstract record Entity : IEntity
{
	[DefaultValue(State.Default), JsonIgnore, Persistence(PersistenceMode.InMemory)]
	public State State { get; init; }
}

public abstract record Entity<T> : Entity, IEntity<T>
	where T : notnull
{
	protected Entity()
	{
		Id = default!;
	}

	[PrimaryKey(true), CacheKey, ReturnValue, Ordinal(-10000)]
	public virtual T Id { get; init; }
}
