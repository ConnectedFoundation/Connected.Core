namespace Connected;

/// <summary>
/// Provides contextual information about the caller of a service operation.
/// </summary>
/// <remarks>
/// This interface exposes information about who or what initiated a service operation,
/// including the sender object and the method name being invoked. This context enables
/// operations to make decisions based on the caller's identity or to log detailed
/// information about operation invocations.
/// </remarks>
public interface ICallerContext
{
	/// <summary>
	/// Gets the sender object that initiated the operation.
	/// </summary>
	object? Sender { get; }

	/// <summary>
	/// Gets the name of the method being invoked.
	/// </summary>
	string? Method { get; }
}
