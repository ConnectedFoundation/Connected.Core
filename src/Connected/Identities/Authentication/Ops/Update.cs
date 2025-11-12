using Connected.Identities.Authentication.Dtos;
using Connected.Notifications;
using Connected.Services;
using Connected.Storage;

namespace Connected.Identities.Authentication.Ops;

internal class Update(IStorageProvider storage, IIdentityAuthenticationTokenCache cache, IEventService events, IIdentityAuthenticationTokenService tokens)
  : ServiceAction<IUpdateIdentityAuthenticationTokenDto>
{
	protected override async Task OnInvoke()
	{
		var entity = SetState(await tokens.Select(Dto)) as IdentityAuthenticationToken ?? throw new NullReferenceException(Connected.Strings.ErrEntityExpected);

		await storage.Open<IdentityAuthenticationToken>().Update(entity.Merge(Dto, Entities.State.Update), Dto, async () =>
		{
			await cache.Remove(Dto.Id);

			return SetState(await tokens.Select(Dto)) as IdentityAuthenticationToken ?? throw new NullReferenceException(Connected.Strings.ErrEntityExpected);
		}, Caller);

		await cache.Remove(Dto.Id);
		await events.Updated(this, tokens, entity.Id);
	}
}
