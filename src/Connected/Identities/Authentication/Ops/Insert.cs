using Connected.Entities;
using Connected.Identities.Authentication.Dtos;
using Connected.Notifications;
using Connected.Services;
using Connected.Storage;

namespace Connected.Identities.Authentication.Ops;

internal class Insert(IStorageProvider storage, IEventService events, IIdentityAuthenticationTokenService tokens)
  : ServiceFunction<IInsertIdentityAuthenticationTokenDto, long>
{
	protected override async Task<long> OnInvoke()
	{
		var entity = await storage.Open<IdentityAuthenticationToken>().Update(Dto.AsEntity<IdentityAuthenticationToken>(State.Add)) ?? throw new NullReferenceException(Connected.Strings.ErrEntityExpected);

		SetState(entity);

		await events.Inserted(this, tokens, entity.Id);

		return entity.Id;
	}
}
