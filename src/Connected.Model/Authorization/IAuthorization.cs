namespace Connected.Authorization;

/// <summary>
/// Represents the outcome of an authorization check used to control downstream processing.
/// </summary>
public enum AuthorizationResult
{
	/// <summary>
	/// No decision made by this handler; defer to other handlers.
	/// </summary>
	Skip = 0,
	/// <summary>
	/// Authorization passed; allow the operation to proceed.
	/// </summary>
	Pass = 1,
	/// <summary>
	/// Authorization failed; deny the operation.
	/// </summary>
	Fail = 2
}
/// <summary>
/// Base authorization middleware contract.
/// </summary>
public interface IAuthorization
	: IMiddleware
{
	/// <summary>
	/// Gets a value indicating whether this authorization instance is sealed and cannot be modified.
	/// </summary>
	bool IsSealed { get; }
	/*
	 * Implementations may expose additional behavior; this interface couples with IMiddleware so that
	 * authorization components can participate in initialization and disposal within the pipeline.
	 */
}