using Connected.Entities;
using Connected.Services.Validation;

namespace Connected.Storage.Sharding.Nodes.Validation;

internal sealed class UpdateShardingNodeValidator(IShardingNodeCache cache)
	: Validator<IUpdateShardingNodeDto>
{
	protected override async Task OnInvoke()
	{
		var existing = await cache.AsEntity(f => string.Equals(f.ConnectionString, Dto.ConnectionString, StringComparison.Ordinal));

		if (existing is not null && existing.Id != Dto.Id)
			throw ValidationExceptions.ValueExists(nameof(Dto.ConnectionString), null);
	}
}