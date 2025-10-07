using Connected.Caching;

namespace Connected.Net.Routing.Server;
internal sealed class RouteCache(ICachingService cachingService)
	: CacheContainer<Route, Guid>(cachingService, NetMetaData.RouteKey), IRouteCache
{
}
