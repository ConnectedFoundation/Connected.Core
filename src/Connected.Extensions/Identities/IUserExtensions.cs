using Connected.Annotations;
using Connected.Identities.Dtos;

namespace Connected.Identities;

[Service, ServiceUrl(IdentitiesUrls.Users)]
public interface IUserExtensions
{
	[ServiceOperation(ServiceOperationVerbs.Post)]
	Task<string?> Validate(IValidateUserDto dto);
}