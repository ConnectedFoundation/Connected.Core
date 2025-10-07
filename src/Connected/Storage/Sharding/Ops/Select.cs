using Connected.Entities;
using Connected.Services;

namespace Connected.Storage.Sharding.Ops;

internal sealed class Select(IShardingCache cache)
	: ServiceFunction<ISelectShardDto, IShard?>
{
	protected override async Task<IShard?> OnInvoke()
	{
		return await cache.AsEntity(f => string.Equals(f.Entity, Dto.Entity, StringComparison.OrdinalIgnoreCase)
			&& string.Equals(f.EntityId, Dto.EntityId, StringComparison.OrdinalIgnoreCase));
	}
}