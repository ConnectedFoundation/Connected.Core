using Connected.Services;
using System.ComponentModel.DataAnnotations;

namespace Connected.Identities.TwoFactor.Dtos;

internal sealed class VerifyTwoFactorDto : Dto, IVerifyTwoFactorDto
{
	[Required, MaxLength(128)]
	public required string Challenge { get; set; }

	[Required, MaxLength(16)]
	public required string Code { get; set; }
}
