using Connected.Annotations;
using Microsoft.AspNetCore.Http;
using System.Collections.Immutable;
using System.Reflection;

namespace Connected.Net.Rest;

[Service]
internal interface IResolutionService
{
	InvokeDescriptor? ResolveMethod(HttpContext context);
	Type? ResolveDto(ParameterInfo parameter);
	ImmutableList<Tuple<string, ServiceOperationVerbs>> QueryRoutes();
}
