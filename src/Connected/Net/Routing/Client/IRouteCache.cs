using Connected.Caching;

namespace Connected.Net.Routing.Client;
internal interface IRouteCache : ICacheContainer<Route, Guid>
{
}
