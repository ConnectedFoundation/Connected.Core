using Connected.Caching;
using Connected.Storage;

namespace Connected.Globalization.Languages;
internal sealed class LanguageCache(ICachingService cache, IStorageProvider storage)
	: EntityCache<ILanguage, Language, int>(cache, storage, MetaData.LanguageKey), ILanguageCache
{
}
