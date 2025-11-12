using Connected.Annotations;
using Connected.Annotations.Entities;

namespace Connected.Entities;
/// <inheritdoc cref="IEntityContainer{TPrimaryKey}"/>>
public abstract record EntityContainer<TPrimaryKey> : ConsistentEntity<TPrimaryKey>, IEntityContainer<TPrimaryKey>
	where TPrimaryKey : notnull
{
	/// <inheritdoc cref="IEntityContainer{TPrimaryKey}.Entity"/>>
	[Ordinal(-10), Length(1024)]
	public virtual string Entity { get; init; } = default!;
	/// <inheritdoc cref="IEntityContainer{TPrimaryKey}.EntityId"/>>
	[Ordinal(-9), Length(128)]
	public virtual string EntityId { get; init; } = default!;
}
