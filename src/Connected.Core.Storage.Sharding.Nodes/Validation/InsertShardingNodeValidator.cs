using Connected.Entities;
using Connected.Validation;

namespace Connected.Storage.Sharding.Nodes.Validation;

internal sealed class InsertShardingNodeValidator(IShardingNodeCache cache) : Validator<IInsertShardingNodeDto>
{
	protected override async Task OnInvoke()
	{
		var existing = await cache.AsEntity(f => string.Equals(f.ConnectionString, Dto.ConnectionString, StringComparison.Ordinal));

		if (existing is not null)
			throw ValidationExceptions.ValueExists(nameof(Dto.ConnectionString), null);
	}
}