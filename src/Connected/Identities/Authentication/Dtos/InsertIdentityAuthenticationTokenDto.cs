using System.ComponentModel.DataAnnotations;

namespace Connected.Identities.Authentication.Dtos;

internal sealed class InsertIdentityAuthenticationTokenDto : IdentityAuthenticationTokenDto, IInsertIdentityAuthenticationTokenDto
{
	[Required, MaxLength(256)]
	public required string Identity { get; set; }

	[Required, MaxLength(256)]
	public required string Key { get; set; }
}
