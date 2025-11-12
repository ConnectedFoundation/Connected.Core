using Connected.Annotations;
using Connected.Identities;
using Connected.Services;

namespace Connected.Authentication.Dtos;
internal sealed class UpdateIdentityDto : Dto, IUpdateIdentityDto
{
	[SkipValidation]
	public IIdentity? Identity { get; set; }
}
