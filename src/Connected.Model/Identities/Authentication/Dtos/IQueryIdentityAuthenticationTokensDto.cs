using Connected.Services;

namespace Connected.Identities.Authentication.Dtos;

public interface IQueryIdentityAuthenticationTokensDto : IDto
{
	string? Identity { get; set; }
	string? Key { get; set; }
}