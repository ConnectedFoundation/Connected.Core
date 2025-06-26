using Connected.Annotations;

namespace Connected.Identities.Authentication.Dtos;

internal sealed class UpdateIdentityAuthenticationTokenDto : IdentityAuthenticationTokenDto, IUpdateIdentityAuthenticationTokenDto
{
	[MinValue(1)]
	public long Id { get; set; }
}
