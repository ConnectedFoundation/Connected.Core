namespace Connected.Entities;

/// <summary>
/// Represents an entity that is split into a lightweight head portion and a
/// separate primary identity. This pattern is commonly used by distributed
/// or sharded storage models where the "head" contains routing or partitioning
/// information while the primary key identifies the concrete record.
/// </summary>
/// <typeparam name="THead">The type of the head value. Must be non-nullable.</typeparam>
/// <typeparam name="TPrimaryKey">The type of the primary key. Must be non-nullable.</typeparam>
/// <remarks>
/// Implementations of <see cref="IDistributedEntity{THead,TPrimaryKey}"/> also
/// implement <see cref="IPrimaryKeyEntity{TPrimaryKey}"/> and the base
/// <see cref="IEntity"/> contract. The <see cref="Head"/> property is
/// intended to carry metadata used for distribution (for example a tenant id,
/// shard key or aggregate header) while the primary key uniquely identifies the
/// record within its partition.
/// </remarks>
public interface IDistributedEntity<THead, TPrimaryKey> : IEntity, IPrimaryKeyEntity<TPrimaryKey>
	where THead : notnull
	where TPrimaryKey : notnull
{
	/// <summary>
	/// Gets the head value used for distribution or routing purposes.
	/// </summary>
	/// <remarks>
	/// The concrete meaning of the head value depends on the storage strategy
	/// and application domain (for example tenant id, shard key, stream header,
	/// etc.). Implementations should treat this value as part of the entity's
	/// identity for distribution but not as the primary key of the record.
	/// </remarks>
	THead Head { get; init; }
}
