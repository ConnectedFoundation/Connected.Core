using Connected.Services;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Connected.Identities.Dtos;
internal sealed class ValidateUserDto
    : Dto, IValidateUserDto
{
    public bool Permanent { get; set; }

    [Required, MaxLength(DefaultIdentityLength)]
    public required string User { get; set; }

    [MaxLength(DefaultTagsLength)]
    public string? Password { get; set; }
}
