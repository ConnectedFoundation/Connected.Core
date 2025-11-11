using Connected.Services;

namespace Connected.Authorization.Services;
/// <summary>
/// DTO carrying caller context and strongly-typed operation data for service operation authorization.
/// </summary>
/// <typeparam name="TDto">The operation DTO type.</typeparam>
public interface IServiceOperationAuthorizationDto<TDto>
	: IDto
	where TDto : IDto
{
	/// <summary>
	/// Gets or sets the caller context for the operation.
	/// </summary>
	ICallerContext Caller { get; set; }
	/// <summary>
	/// Gets or sets the strongly-typed operation DTO payload.
	/// </summary>
	TDto Dto { get; set; }
}