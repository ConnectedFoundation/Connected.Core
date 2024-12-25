using Connected.Services;

namespace Connected.Identities.Authentication;

public interface IUpdateIdentityAuthenticationTokenDto : IDto
{
	long Id { get; set; }
	string? Token { get; set; }
	AuthenticationTokenStatus Status { get; set; }
	DateTimeOffset? Expire { get; set; }
}