using Connected.Caching;
using Connected.Storage;

namespace Connected.Globalization.Languages.Mappings;
internal sealed class LanguageMappingCache(ICachingService cache, IStorageProvider storage)
	: EntityCache<LanguageMapping, int>(cache, storage, MetaData.LanguageMappingKey), ILanguageMappingCache
{
}
