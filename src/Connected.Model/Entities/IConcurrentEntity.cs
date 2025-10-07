using Connected.Annotations.Entities;

namespace Connected.Entities;

/// <summary>
/// This entity is primarly used when cached in memory and access to the entity is frequent with
/// updates. The <see cref="IConcurrentEntity{TPrimaryKey}"/> ensures that threads don't overwrite
/// values from other threads by using the <see cref="Sync"/> property.
/// </summary>
/// <remarks>
/// While <see cref="IConsistentEntity{T}"/> ensures database consistency, <see cref="IConcurrentEntity{TPrimaryKey}"/>
/// ensures application consistency. The <see cref="IConcurrentEntity{TPrimaryKey}"/> is not thread safe but ensures
/// that any writes are rejected if the thread tries to write entity with invalid <see cref="Sync"/> property value.
/// </remarks>
/// <typeparam name="TPrimaryKey"></typeparam>
public interface IConcurrentEntity<TPrimaryKey> : IConsistentEntity<TPrimaryKey>
	 where TPrimaryKey : notnull
{
	/// <summary>
	/// The synchronization value used when comparing if the write to the entity is made with the latest
	/// version. Entities are immutable but they can be replaced in Cache with newer instances. The cache tipically
	/// ensures that entities can't be overwritten with out of date values.
	/// </summary>
	/// <example>
	/// In Queue messages, all messages are stored in memory and multiple threads perform dequeue. Since dequeue means
	/// overwriting some data and since the entities are immutable (except this entity and only the <see cref="Sync"/> property) the operaton results with overwriting the entire
	/// entity in cache. If two or more thready do it in the same time, they could accidentally overwrite values from
	/// each other. The cache ensures that the current entity has the same value as the updating entity. If the write
	/// occurred in the mean time it would result incrementing the <see cref="Sync"/> value which would cause any subsequent
	/// writes with the same originating entity would fail.
	/// </example>
	[Persistence(PersistenceMode.InMemory)]
	int Sync { get; set; }
}
