using Connected.Services;
using System.ComponentModel.DataAnnotations;

namespace Connected.Identities.TwoFactor.Dtos;

internal sealed class ChallengeTwoFactorDto : Dto, IChallengeTwoFactorDto
{
	[Range(1, long.MaxValue)]
	public long User { get; set; }

	public bool Permanent { get; set; }
}
