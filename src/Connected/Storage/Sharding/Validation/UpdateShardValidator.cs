using Connected.Entities;
using Connected.Services.Validation;

namespace Connected.Storage.Sharding.Validation;

internal sealed class UpdateShardValidator(IShardingCache cache) : Validator<IUpdateShardDto>
{
	protected override async Task OnInvoke()
	{
		var existing = await cache.AsEntity(f => string.Equals(f.Entity, Dto.Entity, StringComparison.Ordinal)
			&& string.Equals(f.EntityId, Dto.EntityId, StringComparison.OrdinalIgnoreCase));

		if (existing is not null && existing.Id != Dto.Id)
			throw ValidationExceptions.ValueExists($"{nameof(Dto.Entity)}, {nameof(Dto.EntityId)}", $"{Dto.Entity}, {Dto.EntityId}");
	}
}