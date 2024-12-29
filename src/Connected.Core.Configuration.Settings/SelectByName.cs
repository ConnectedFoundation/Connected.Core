using Connected.Services;

namespace Connected.Configuration.Settings;

internal sealed class SelectByName(ISettingCache cache) : ServiceFunction<INameDto, ISetting?>
{
	protected override Task<ISetting?> OnInvoke()
	{
		return Task.FromResult<ISetting?>(cache.FirstOrDefault(f => string.Equals(f.Name, Dto.Name, StringComparison.OrdinalIgnoreCase)));
	}
}
