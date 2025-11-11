namespace Connected.Authorization.Net;

/// <summary>
/// Defines an authorization component that evaluates the current HTTP request context
/// and returns an <see cref="AuthorizationResult"/> indicating whether processing should
/// proceed, be denied, or be delegated to other handlers.
/// </summary>
public interface IHttpRequestAuthorization
	: IAuthorization
{
	/// <summary>
	/// Performs authorization against the current HTTP request context.
	/// </summary>
	/// <returns>An <see cref="AuthorizationResult"/> value: Pass, Fail, or Skip.</returns>
	Task<AuthorizationResult> Invoke();

	/*
	 * Contract-only interface: concrete implementations should read request details (headers,
	 * route, identity) and compute an authorization result to guide pipeline execution.
	 */
}