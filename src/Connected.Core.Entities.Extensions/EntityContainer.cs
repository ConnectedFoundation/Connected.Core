using Connected.Annotations;
using Connected.Annotations.Entities;

namespace Connected.Entities;
/// <inheritdoc cref="IEntityContainer{TPrimaryKey}"/>>
public abstract record EntityContainer<TPrimaryKey> : ConsistentEntity<TPrimaryKey>, IEntityContainer<TPrimaryKey>
	where TPrimaryKey : notnull
{
	/// <inheritdoc cref="IEntityContainer{TPrimaryKey}.Entity"/>>
	[Ordinal(-10), Length(128)]
	public string Entity { get; init; } = default!;
	/// <inheritdoc cref="IEntityContainer{TPrimaryKey}.EntityId"/>>
	[Ordinal(-9), Length(128)]
	public string EntityId { get; init; } = default!;
}
