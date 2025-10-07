using Connected.Annotations.Entities;
using Connected.Net.Routing;

namespace Connected.Net;
public static class NetMetaData
{
	public const string RouteKey = $"{SchemaAttribute.CoreSchema}.{nameof(IRoute)}";
}
