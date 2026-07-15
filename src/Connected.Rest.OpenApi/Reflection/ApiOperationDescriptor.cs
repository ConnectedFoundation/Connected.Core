using Connected.Annotations;
using System.Reflection;

namespace Connected.Net.Rest.OpenApi.Reflection;

/// <summary>
/// Describes a single Http-accessible service operation, resolved the same way the REST
/// resolution pipeline resolves it: a <see cref="ServiceAttribute"/>-decorated service
/// interface, one of its methods decorated with <see cref="ServiceOperationAttribute"/>.
/// </summary>
internal sealed record ApiOperationDescriptor(string Url, ServiceOperationVerbs Verbs, Type Service, MethodInfo Method);
