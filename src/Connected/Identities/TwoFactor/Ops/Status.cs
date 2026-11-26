using Connected.Services;
using Connected.Storage;

namespace Connected.Identities.TwoFactor.Ops;

internal sealed class Status(IStorageProvider storage)
	: ServiceFunction<IPrimaryKeyDto<long>, TwoFactorStatus>
{
	protected override async Task<TwoFactorStatus> OnInvoke()
	{
		return (await TwoFactorStore.Factor(storage, Dto.Id))?.Status ?? TwoFactorStatus.None;
	}
}
