using Connected.Caching;

namespace Connected.Identities.MetaData;

internal interface IIdentityMetaDataCache
  : IEntityCache<IdentityMetaData, string>
{
}
