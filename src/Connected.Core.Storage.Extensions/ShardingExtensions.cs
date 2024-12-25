using Connected.Entities;
using Connected.Services;
using Connected.Storage.Sharding;
using Connected.Storage.Sharding.Nodes;

namespace Connected.Storage;
/// <summary>
/// Provides extension methods for managing shards and nodes.
/// </summary>
public static class ShardingExtensions
{
	/// <summary>
	/// Selects the most appropriate node for a potentialy new shard.
	/// </summary>
	/// <param name="shards">The list of existing Shards. The list should contain only
	/// one Entity set.</param>
	/// <param name="nodes">The list of all available Sharding Nodes.</param>
	public static IShardingNode? ChooseNodeCandidate(this ImmutableList<IShard> shards, ImmutableList<IShardingNode> nodes)
	{
		/*
       * First, group by nodes
       */
		var groups = shards.GroupBy(f => f.Node);
		/*
       * Now check if any of nodes don't have records yet.
       */
		foreach (var node in nodes)
		{
			if (!groups.Any(f => f.Key == node.Id))
				return node;
		}
		/*
       * All nodes have records, let's select that which have the least records
       */
		var candidateList = groups.Select(f => new { Id = f.Key, Count = f.Count() }).OrderBy(f => f.Count);

		foreach (var candidate in candidateList)
		{
			if (nodes.FirstOrDefault(f => f.Id == candidate.Id) is IShardingNode node && node.Status == Status.Enabled)
				return node;
		}

		return null;
	}
	/// <summary>
	/// Resolves sharding node(s) for the specified operation.
	/// </summary>
	/// <remarks>
	/// This method tries to resolve in which sharding node(s) the data resides and returns those shards.
	/// It looks in the parameters and variables collections and tries to find sharding nodes that match the criteria
	/// </remarks>
	/// <param name="sharding">The sharding service providing the the pointers to the nodes.</param>
	/// <param name="nodes">The nodes service providing collection of existing nodes.</param>
	/// <param name="operation">The storage operation which will be executed on returned nodes.</param>
	/// <param name="propertyName">The property name that will be matched with parameters and variables.</param>
	public static async Task<ImmutableList<IShardingNode>> ResolveNode<TEntity>(this IShardingService sharding, IShardingNodeService nodes, IStorageOperation operation, string propertyName)
		where TEntity : IEntity
	{
		var entityName = typeof(TEntity).FullName;

		if (entityName is null)
			throw new NullReferenceException($"{Strings.ErrCannotResolveTypeName} ('{typeof(TEntity)}')");
		/*
		 * First look in the parameters list.
		 */
		var parameter = operation.ResolveParameter(propertyName);
		string? value = null;

		if (parameter is null || parameter.Value is null)
		{
			/*
			 * If parameter does not exist the second chance is in the 
			 * variables.
			 */
			var variable = operation.ResolveVariable(propertyName);

			if (variable is not null)
			{
				var firstNonNull = variable.Values?.FirstOrDefault(f => f is not null);

				if (firstNonNull is not null)
					value = firstNonNull.ToString();
			}
		}

		if (value is null)
		{
			/*
			 * Value still not resolved. This is the last chance.
			 */
			if (parameter?.Value is not null)
				value = parameter.Value.ToString();
		}
		/*
		 * Did not find anything useful. Return empty list which will later probably throw
		 * exception somewhere along the way.
		 */
		if (value is null)
			return ImmutableList<IShardingNode>.Empty;
		/*
		 * We have a value so there a good chance wi'll get the node.
		 */
		var dto = await Scope.ResolveDto<ISelectShardDto>();

		dto.Entity = entityName;
		dto.EntityId = value;

		var result = await sharding.Select(dto);
		/*
		 * Nope, it doesn't exists which is perfectly legal because the data could be in the default
		 * storage so we'll return a default sharding node which points to the default (non sharded)
		 * storage.
		 */
		if (result is null)
			return new List<IShardingNode> { new DefaultShardingNode() }.ToImmutableList();
		/*
		 * We have a node, one more check to see if it actually exists or we have a void pointer.
		 */
		var node = await nodes.Select(new PrimaryKeyDto<int> { Id = result.Node });

		if (node is null)
			throw new NullReferenceException($"{Strings.ErrShardingNodeNotFound} ('{result.Node}')");
		/*
		 * Everything ok, we've got a valid node.
		 */
		return [node];
	}
	/// <summary>
	/// Attaches the default sharding node the the existing set of nodes.
	/// </summary>
	/// <remarks>
	/// Use this method when you want to perform a storage operation in the default storage as part of 
	/// the execution process.
	/// </remarks>
	public static ImmutableList<IShardingNode> WithDefaultNode(this ImmutableList<IShardingNode> existing)
	{
		var result = new List<IShardingNode>(existing.ToArray());

		result.Append(new DefaultShardingNode());

		return result.ToImmutableList();
	}
}