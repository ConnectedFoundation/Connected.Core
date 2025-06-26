using Connected.Entities;
using Connected.Notifications;
using Connected.Services;
using Connected.Storage;

namespace Connected.Identities.Authentication.Ops;

internal sealed class Delete(IStorageProvider storage, IIdentityAuthenticationTokenCache cache, IEventService events, IIdentityAuthenticationTokenService tokens)
  : ServiceAction<IPrimaryKeyDto<long>>
{
	protected override async Task OnInvoke()
	{
		var entity = SetState(await tokens.Select(Dto)) as IdentityAuthenticationToken ?? throw new NullReferenceException(Connected.Strings.ErrEntityExpected);

		await storage.Open<IdentityAuthenticationToken>().Update(entity.Merge(Dto, State.Delete));
		await cache.Remove(Dto.Id);
		await events.Deleted(this, tokens, Dto.Id);
	}
}
