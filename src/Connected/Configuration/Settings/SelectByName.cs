using Connected.Entities;
using Connected.Services;

namespace Connected.Configuration.Settings;

internal sealed class SelectByName(ISettingCache cache) : ServiceFunction<INameDto, ISetting?>
{
	protected override async Task<ISetting?> OnInvoke()
	{
		return await cache.AsEntity(f => string.Equals(f.Name, Dto.Name, StringComparison.OrdinalIgnoreCase));
	}
}
