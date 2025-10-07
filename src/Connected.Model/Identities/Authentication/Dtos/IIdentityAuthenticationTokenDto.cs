using Connected.Services;

namespace Connected.Identities.Authentication.Dtos;

public interface IIdentityAuthenticationTokenDto : IDto
{
	string? Token { get; set; }
	AuthenticationTokenStatus Status { get; set; }
	DateTimeOffset? Expire { get; set; }

}
