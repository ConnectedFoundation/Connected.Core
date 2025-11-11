using Connected.Authorization.Dtos;

namespace Connected.Authorization;

/// <summary>
/// Middleware contract for handling authorization decoration, invoking an authorization component
/// using a decoration handler DTO.
/// </summary>
public interface IAuthorizationDecorationHandler
	: IMiddleware
{
	/// <summary>
	/// Executes decoration-based authorization logic.
	/// </summary>
	/// <param name="dto">DTO containing middleware to invoke and related context.</param>
	/// <returns>Authorization result produced by the decorated middleware.</returns>
	Task<AuthorizationResult> Invoke(IAuthorizationDecorationHandlerDto dto);
	/*
	 * Implementations bridge decorated authorization components into the runtime pipeline.
	 */
}
