namespace Connected.Annotations;
/// <summary>
/// Associates a stable identifier with a middleware type to support discovery and referencing.
/// </summary>
[AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
public sealed class MiddlewareIdAttribute(string id)
	: Attribute
{
	/*
	 * Persist the identifier to allow lookup or filtering of middleware components by id.
	 */
	/// <summary>
	/// Gets the middleware identifier.
	/// </summary>
	public string Id { get; } = id;
}
