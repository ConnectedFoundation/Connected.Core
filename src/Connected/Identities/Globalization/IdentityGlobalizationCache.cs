using Connected.Caching;
using Connected.Storage;

namespace Connected.Identities.Globalization;

internal sealed class IdentityGlobalizationCache(ICachingService cache, IStorageProvider storage)
	: EntityCache<IIdentityGlobalization, IdentityGlobalization, string>(cache, storage, IdentitiesMetaData.GlobalizationKey), IIdentityGlobalizationCache
{
}