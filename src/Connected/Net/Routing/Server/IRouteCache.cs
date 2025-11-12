using Connected.Caching;

namespace Connected.Net.Routing.Server;
internal interface IRouteCache : ICacheContainer<Route, Guid>
{
}
