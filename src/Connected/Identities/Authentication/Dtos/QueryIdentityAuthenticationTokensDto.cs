using Connected.Services;
using System.ComponentModel.DataAnnotations;

namespace Connected.Identities.Authentication.Dtos;

internal sealed class QueryIdentityAuthenticationTokensDto : Dto, IQueryIdentityAuthenticationTokensDto
{
	[MaxLength(256)]
	public string? Identity { get; set; }

	[MaxLength(256)]
	public string? Key { get; set; }
}
