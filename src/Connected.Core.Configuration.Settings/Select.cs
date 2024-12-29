using Connected.Services;

namespace Connected.Configuration.Settings;

internal sealed class Select(ISettingCache cache) : ServiceFunction<IPrimaryKeyDto<int>, ISetting?>
{
	protected override Task<ISetting?> OnInvoke()
	{
		return Task.FromResult<ISetting?>(cache.FirstOrDefault(f => f.Id == Dto.Id));
	}
}
