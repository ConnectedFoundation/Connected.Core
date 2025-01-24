using Connected.Caching;

namespace Connected.Core.Routing;
internal interface IRouteCache : ICacheContainer<Route, Guid>
{
}
