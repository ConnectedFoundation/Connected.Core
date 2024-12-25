using Connected.Services;

namespace Connected.Identities.Authentication;

public interface IQueryIdentityAuthenticationTokensDto : IDto
{
	string? Identity { get; set; }
	string? Key { get; set; }
}