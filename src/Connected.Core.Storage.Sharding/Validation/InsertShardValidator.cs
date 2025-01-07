using Connected.Entities;
using Connected.Validation;

namespace Connected.Storage.Sharding.Validation;

internal sealed class InsertShardValidator(IShardingCache cache) : Validator<IInsertShardDto>
{
	protected override async Task OnInvoke()
	{
		var existing = await cache.AsEntity(f => string.Equals(f.Entity, Dto.Entity, StringComparison.Ordinal)
			&& string.Equals(f.EntityId, Dto.EntityId, StringComparison.OrdinalIgnoreCase));

		if (existing is not null)
			throw ValidationExceptions.ValueExists($"{nameof(Dto.Entity)}, {nameof(Dto.EntityId)}", $"{Dto.Entity}, {Dto.EntityId}");
	}
}