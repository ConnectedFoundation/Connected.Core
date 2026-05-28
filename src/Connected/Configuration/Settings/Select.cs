using Connected.Entities;
using Connected.Services;

namespace Connected.Configuration.Settings;

internal sealed class Select(ISettingCache cache) : ServiceFunction<IPrimaryKeyDto<int>, ISetting?>
{
	protected override async Task<ISetting?> OnInvoke()
	{
		return await cache.AsEntity(f => f.Id == Dto.Id);
	}
}
