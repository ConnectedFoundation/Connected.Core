namespace Connected.Services;

/// <summary>
/// Represents a data transfer object containing a distributed primary key.
/// </summary>
/// <typeparam name="THead">The type of the head identifier for distributed key resolution.</typeparam>
/// <typeparam name="TPrimaryKey">The type of the primary key identifier.</typeparam>
/// <remarks>
/// This interface extends the basic primary key DTO with a head identifier, enabling
/// identification of entities in distributed systems where data is partitioned across
/// multiple nodes, shards, or databases. The head typically identifies the partition
/// or location where the entity resides.
/// </remarks>
public interface IDistributedPrimaryKeyDto<THead, TPrimaryKey>
	: IPrimaryKeyDto<TPrimaryKey>
	where THead : notnull
	where TPrimaryKey : notnull
{
	/// <summary>
	/// Gets or sets the head identifier for distributed key resolution.
	/// </summary>
	THead Head { get; set; }
}
