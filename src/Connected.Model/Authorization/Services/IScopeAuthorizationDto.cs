using Connected.Services;

namespace Connected.Authorization.Services;

/// <summary>
/// DTO carrying caller and operation context for scope-based authorization decisions.
/// </summary>
public interface IScopeAuthorizationDto
	: IDto
{
	/// <summary>
	/// Gets or sets context describing who invoked the operation and from where.
	/// </summary>
	ICallerContext Caller { get; set; }
	/// <summary>
	/// Gets or sets the operation DTO representing the requested action.
	/// </summary>
	IDto Dto { get; set; }
}
