namespace Connected.Annotations;
/// <summary>
/// Specifies that a type will inject into a specific pipeline.
/// </summary>
/// <remarks>
/// Some middleware components must be bound to the specific component
/// in order for Connected to recognize them. ServiceAuthorization middleware, 
/// for example, will specify a service as a TComponent, 
/// whereas a ServiceOperationAuthorization middleware will in addition to the TComponent specify
/// also a method (a Service Operation) on which authorization would like to perform.
/// </remarks>
/// <typeparam name="TComponent">A component's type to which a middleware is bound to.</typeparam>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
public sealed class MiddlewareAttribute<TComponent>
	: Attribute
{
	/// <summary>
	/// Creates a new instance of the MiddlewareAttribute class.
	/// </summary>
	public MiddlewareAttribute()
	{
		/*
		 * Default constructor: middleware binds to the component type as a whole.
		 */
	}
	/// <summary>
	/// Creates a new instance of the MiddlewareAttribute class with a specific method attached.
	/// </summary>
	/// <param name="method">A method name in the component to which a middleware will be bound.</param>
	public MiddlewareAttribute(string? method)
	{
		/*
		 * Allow targeting a specific method within the component for fine-grained pipeline injection.
		 */
		Method = method;
	}
	/// <summary>
	/// Gets a method name to which a middleware is bound.
	/// </summary>
	public string? Method { get; }
}
