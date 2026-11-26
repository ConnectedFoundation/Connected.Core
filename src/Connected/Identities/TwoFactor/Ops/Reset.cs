using Connected.Entities;
using Connected.Services;
using Connected.Storage;

namespace Connected.Identities.TwoFactor.Ops;

internal sealed class Reset(IStorageProvider storage)
	: ServiceAction<IPrimaryKeyDto<long>>
{
	protected override async Task OnInvoke()
	{
		await TwoFactorStore.DeleteChallenges(storage, Dto.Id);

		if (await TwoFactorStore.Factor(storage, Dto.Id) is UserTwoFactor factor)
			await storage.Open<UserTwoFactor>().Update(factor with { State = State.Delete });
	}
}
