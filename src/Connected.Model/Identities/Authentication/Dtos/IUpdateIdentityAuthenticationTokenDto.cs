using Connected.Services;

namespace Connected.Identities.Authentication.Dtos;

/// <summary>
/// Represents a data transfer object for updating an existing identity authentication token.
/// </summary>
/// <remarks>
/// This interface combines the base authentication token properties with primary key
/// identification to enable token updates. The long-typed primary key uniquely identifies
/// the token record to be modified.
/// </remarks>
public interface IUpdateIdentityAuthenticationTokenDto
	: IIdentityAuthenticationTokenDto, IPrimaryKeyDto<long>
{
}