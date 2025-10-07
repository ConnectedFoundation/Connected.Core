using Connected.Services;

namespace Connected.Identities.Authentication.Dtos;

public interface IUpdateIdentityAuthenticationTokenDto : IIdentityAuthenticationTokenDto, IPrimaryKeyDto<long>
{
}