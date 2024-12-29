using Connected.Caching;
using Connected.Storage;

namespace Connected.Configuration.Settings;

internal sealed class SettingCache(ICachingService cache, IStorageProvider storage)
	: EntityCache<Setting, int>(cache, storage, Setting.EntityKey), ISettingCache
{
}
