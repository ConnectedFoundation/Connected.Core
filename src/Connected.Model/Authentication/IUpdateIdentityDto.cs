using Connected.Identities;
using Connected.Services;

namespace Connected.Authentication;
public interface IUpdateIdentityDto : IDto
{
	IIdentity? Identity { get; set; }
}
