using Connected.Annotations;
using Connected.Annotations.Entities;

namespace Connected.Entities;

public abstract record DependentEntity<THead, TPrimaryKey> : Entity<TPrimaryKey>
	where THead : notnull
	where TPrimaryKey : notnull
{
	[Index(false), Ordinal(-9999)]
	public required virtual THead Head { get; init; }
}