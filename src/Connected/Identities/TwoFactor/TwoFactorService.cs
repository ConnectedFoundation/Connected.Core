using Connected.Identities.TwoFactor.Dtos;
using Connected.Identities.TwoFactor.Ops;
using Connected.Services;

namespace Connected.Identities.TwoFactor;

internal sealed class TwoFactorService(IServiceProvider services)
	: Service(services), ITwoFactorService
{
	public async Task<TwoFactorStatus> Status(IPrimaryKeyDto<long> dto)
	{
		return await Invoke(GetOperation<Status>(), dto);
	}

	public async Task<SignInResult> Challenge(IChallengeTwoFactorDto dto)
	{
		return await Invoke(GetOperation<Challenge>(), dto);
	}

	public async Task<TwoFactorSetup?> Setup(IValueDto<string> dto)
	{
		return await Invoke(GetOperation<Setup>(), dto);
	}

	public async Task<string?> Verify(IVerifyTwoFactorDto dto)
	{
		return await Invoke(GetOperation<Verify>(), dto);
	}

	public async Task Reset(IPrimaryKeyDto<long> dto)
	{
		await Invoke(GetOperation<Reset>(), dto);
	}
}
