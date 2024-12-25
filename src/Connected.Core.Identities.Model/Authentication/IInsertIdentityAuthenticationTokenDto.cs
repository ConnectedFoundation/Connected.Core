using Connected.Services;

namespace Connected.Identities.Authentication;

public interface IInsertIdentityAuthenticationTokenDto : IDto
{
	string Identity { get; set; }
	string Key { get; set; }
	string? Token { get; set; }
	AuthenticationTokenStatus Status { get; set; }
	DateTimeOffset? Expire { get; set; }
}