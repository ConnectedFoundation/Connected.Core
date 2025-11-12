using Connected.Caching;

namespace Connected.Identities.Authentication;

internal interface IIdentityAuthenticationTokenCache
	: ICacheContainer<IdentityAuthenticationToken, long>
{
}
