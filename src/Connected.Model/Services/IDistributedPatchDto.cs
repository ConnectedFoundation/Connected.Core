namespace Connected.Services;

/// <summary>
/// Represents a data transfer object for partial updates in distributed scenarios.
/// </summary>
/// <typeparam name="THead">The type of the head identifier for distributed key resolution.</typeparam>
/// <typeparam name="TPrimaryKey">The type of the primary key identifier.</typeparam>
/// <remarks>
/// This interface combines distributed primary key identification with patch capabilities,
/// enabling partial updates of entities in distributed systems where records are identified
/// by both a head (partition/shard) identifier and a primary key. The property provider
/// mechanism allows tracking which properties should be updated.
/// </remarks>
public interface IDistributedPatchDto<THead, TPrimaryKey>
	: IDistributedPrimaryKeyDto<THead, TPrimaryKey>, IPatchDto<TPrimaryKey>, IPropertyProvider
	where THead : notnull
	where TPrimaryKey : notnull
{
}
