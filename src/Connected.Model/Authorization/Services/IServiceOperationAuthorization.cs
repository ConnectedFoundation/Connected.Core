using Connected.Services;

namespace Connected.Authorization.Services;
/// <summary>
/// Defines authorization logic for a specific service operation represented by a DTO type.
/// </summary>
/// <typeparam name="TDto">The operation DTO type.</typeparam>
public interface IServiceOperationAuthorization<TDto>
	: IAuthorization
	where TDto : IDto
{
	/// <summary>
	/// Performs authorization for a specific service operation.
	/// </summary>
	/// <remarks>
	/// Implementations use caller context and operation data to decide whether the operation may proceed.
	/// </remarks>
	/// <param name="dto">DTO containing caller context and operation data.</param>
	/// <returns>Authorization decision result.</returns>
	Task<AuthorizationResult> Invoke(IServiceOperationAuthorizationDto<TDto> dto);
	/*
	 * The method returns Pass, Fail, or Skip based on custom logic.
	 */
}