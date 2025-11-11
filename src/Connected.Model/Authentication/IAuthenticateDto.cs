using Connected.Services;

namespace Connected.Authentication;
/// <summary>
/// Data transfer object carrying authentication scheme and token values used during identity resolution.
/// </summary>
/// <remarks>
/// Implementations supply protocol-specific values (e.g., "bearer" plus encoded token) that providers consume
/// to authenticate and establish an identity for the current scope.
/// </remarks>
public interface IAuthenticateDto : IDto
{
	/*
	 * Marker interface members: supply the scheme name and raw token text for middleware providers.
	 * Providers inspect these to decide which authentication logic to apply.
	 */
	/// <summary>
	/// Gets or sets the authentication scheme (e.g., bearer, basic).
	/// </summary>
	string? Schema { get; set; }
	/// <summary>
	/// Gets or sets the encoded token or credential string.
	/// </summary>
	string? Token { get; set; }
}
