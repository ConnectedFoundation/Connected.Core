using Connected.Caching;

namespace Connected.Net.Routing.Client;
internal sealed class RouteCache(ICachingService cachingService)
	: CacheContainer<Route, Guid>(cachingService, NetMetaData.RouteKey), IRouteCache
{
}
