using Connected.Caching;
using Connected.Net;

namespace Connected.Core.Routing;
internal sealed class RouteCache(ICachingService cachingService)
	: CacheContainer<Route, Guid>(cachingService, NetMetaData.RouteKey), IRouteCache
{
}
