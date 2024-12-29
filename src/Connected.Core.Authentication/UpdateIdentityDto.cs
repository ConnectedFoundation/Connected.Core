using Connected.Annotations;
using Connected.Identities;
using Connected.Services;

namespace Connected.Authentication;
internal sealed class UpdateIdentityDto : Dto, IUpdateIdentityDto
{
	[SkipValidation]
	public IIdentity? Identity { get; set; }
}
