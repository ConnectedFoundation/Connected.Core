using Connected.Caching;

namespace Connected.Identities.Authentication;

internal sealed class IdentityAuthenticationTokenCache(ICachingService cachingService)
		: CacheContainer<IdentityAuthenticationToken, long>(cachingService, IdentitiesMetaData.AuthenticationTokenKey), IIdentityAuthenticationTokenCache
{
}
