using Connected.Annotations;
using System.Reflection;

namespace Connected.Net.Rest;

internal class InvokeDescriptor
{
	public Type? Service { get; set; }
	public MethodInfo? Method { get; set; }

	public Type[]? Parameters { get; set; }
	public ServiceOperationVerbs Verbs { get; set; } = ServiceOperationVerbs.None;
}
