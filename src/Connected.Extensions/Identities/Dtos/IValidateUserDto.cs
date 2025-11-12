using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Connected.Identities.Dtos;
public interface IValidateUserDto
    : ISelectUserDto
{
    bool Permanent { get; set; }
}
