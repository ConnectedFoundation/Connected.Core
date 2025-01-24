using Connected.Caching;

namespace Connected.Net.Routing;
internal interface IRouteCache : ICacheContainer<Route, Guid>
{
}
