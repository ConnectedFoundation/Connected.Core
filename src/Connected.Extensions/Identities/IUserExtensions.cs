using Connected.Annotations;
using Connected.Identities.Dtos;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Connected.Identities;

[Service, ServiceUrl(IdentitiesUrls.Users)]
public interface IUserExtensions
{
    [ServiceOperation(ServiceOperationVerbs.Post)]
    Task<string?> Validate(IValidateUserDto dto);
}