using Connected.Caching;

namespace Connected.Configuration.Settings;

internal interface ISettingCache : IEntityCache<ISetting, int>
{
}