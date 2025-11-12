namespace Connected.Annotations;
/// <summary>
/// Specifies whic verbs are allowed on the service operation.
/// </summary>
[Flags]
public enum ServiceOperationVerbs
{
	/// <summary>
	/// Operation does not allow any verbs which means it's not accessibley
	/// by external clients.
	/// </summary>
	None = 0,
	/// <summary>
	/// A GET verb is allowed on the operation.
	/// </summary>
	Get = 1,
	/// <summary>
	/// A POST verb is allowed on the operation.
	/// </summary>
	Post = 2,
	/// <summary>
	/// A PUT verb is allowed on the operation.
	/// </summary>
	Put = 4,
	/// <summary>
	/// A DELETE verb is allowed on the operation.
	/// </summary>
	Delete = 8,
	/// <summary>
	/// A PATCH verb is allowed on the operation.
	/// </summary>
	Patch = 16,
	/// <summary>
	/// An OPTIONS verb is allowed on the operation.
	/// </summary>
	Options = 32,
	/// <summary>
	/// A TRACE verb is allowed on the operation.
	/// </summary>
	Trace = 64
}
/// <summary>
/// Specifies that a Service Operation will be potentially accessible
/// by external clients.
/// </summary>
/// <remarks>
/// Creates a new instance of the ServiceOperationAttribute class.
/// </remarks>
/// <param name="verbs">A set of verbs that will be allowed on the operation.</param>
[AttributeUsage(AttributeTargets.Method)]
public sealed class ServiceOperationAttribute(ServiceOperationVerbs verbs) : Attribute
{
	/// <summary>
	/// Gets a set of allowed verbs on the operation, if any.
	/// </summary>
	public ServiceOperationVerbs Verbs { get; } = verbs;
}
