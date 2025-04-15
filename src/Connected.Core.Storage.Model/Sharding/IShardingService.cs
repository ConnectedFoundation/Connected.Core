using Connected.Annotations;
using Connected.Services;
using System.Collections.Immutable;

namespace Connected.Storage.Sharding;
/// <summary>
/// A services providing a storage for the sharding definitions.
/// </summary>
/// <remarks>
/// This is an internal service normally used in combination with <c>IShardingNodeProvider</c> which
/// provides the logic how the shards should be organized.
/// </remarks>
[Service]
public interface IShardingService
{
	/// <summary>
	/// Returns all shards for the specified entity.
	/// </summary>
	/// <param name="dto">A dto containing the entity for which shards will be returned.</param>
	/// <returns>All shards for the specified entity.</returns>
	Task<IImmutableList<IShard>> Query(IQueryShardsDto dto);
	/// <summary>
	/// Returns a shard for the specified entity and its primary key if exists, or null otherwise.
	/// </summary>
	/// <param name="dto">A dto containing the entity and its primary key for which a shard will be returned.</param>
	/// <returns>A shard for the specified entity and its primary key, if exists.</returns>
	Task<IShard?> Select(ISelectShardDto dto);
	/// <summary>
	/// Inserts a new shard for the specified entity and its primary key.
	/// </summary>
	/// <param name="dto">A dto containing information about the new shard.</param>
	/// <returns>An id of the newly inserted shard.</returns>
	Task<int> Insert(IInsertShardDto dto);
	/// <summary>
	/// Updates the existing shard. This method is intended for migrating data from one node to another.
	/// </summary>
	/// <param name="dto">A dto containing the shard to be updated.</param>
	Task Update(IUpdateShardDto dto);
	/// <summary>
	/// Deletes the shard from the storage.
	/// </summary>
	Task Delete(IPrimaryKeyDto<int> dto);
}
