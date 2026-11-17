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
		/*
		 * Expired tokens are deleted by token authentication and by sign-in, which can race, so deleting a token
		 * that is already gone is not an error.
		 */
		if (SetState(await tokens.Select(Dto)) is not IdentityAuthenticationToken entity)
			return;

		await storage.Open<IdentityAuthenticationToken>().Update(entity.Merge(Dto, State.Delete));
		await cache.Remove(Dto.Id);
		await events.Deleted(this, tokens, Dto.Id);
	}
}
