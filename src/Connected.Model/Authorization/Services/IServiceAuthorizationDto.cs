using Connected.Services;

namespace Connected.Authorization.Services;

/// <summary>
/// DTO providing caller context and payload for service-level authorization.
/// </summary>
public interface IServiceAuthorizationDto : IDto
{
	/// <summary>
	/// Gets or sets the caller context.
	/// </summary>
	ICallerContext Caller { get; set; }

	/// <summary>
	/// Gets or sets the operation DTO payload.
	/// </summary>
	IDto Dto { get; set; }
}