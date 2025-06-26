using Connected.Services;
using System.ComponentModel.DataAnnotations;

namespace Connected.Identities.Authentication.Dtos;

internal abstract class IdentityAuthenticationTokenDto : Dto, IIdentityAuthenticationTokenDto
{
	[MaxLength(256)]
	public string? Token { get; set; }
	public AuthenticationTokenStatus Status { get; set; }
	public DateTimeOffset? Expire { get; set; }
}
