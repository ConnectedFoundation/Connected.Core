namespace Connected.Services;

/// <summary>
/// Represents a data transfer object containing a list of distributed primary keys.
/// </summary>
/// <typeparam name="THead">The type of the head identifier for distributed key resolution.</typeparam>
/// <typeparam name="TPrimaryKey">The type of the primary key identifier.</typeparam>
/// <remarks>
/// This interface provides a collection of distributed keys, where each item is a tuple
/// combining a head identifier with a primary key. This enables batch operations on
/// entities across multiple partitions or shards in distributed systems.
/// </remarks>
public interface IDistributedPrimaryKeyListDto<THead, TPrimaryKey>
	: IDto
	where THead : notnull
	where TPrimaryKey : notnull
{
	/// <summary>
	/// Gets or sets the list of distributed key tuples, each containing a head and primary key pair.
	/// </summary>
	List<Tuple<THead, TPrimaryKey>> Items { get; set; }
}
