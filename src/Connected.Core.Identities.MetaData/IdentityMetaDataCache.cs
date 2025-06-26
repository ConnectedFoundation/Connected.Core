using Connected.Caching;
using Connected.Storage;

namespace Connected.Identities.MetaData;

internal sealed class IdentityMetaDataCache(ICachingService cache, IStorageProvider storage)
	 : EntityCache<IdentityMetaData, string>(cache, storage, IdentitiesMetaData.MetaDataKey)
{
}
