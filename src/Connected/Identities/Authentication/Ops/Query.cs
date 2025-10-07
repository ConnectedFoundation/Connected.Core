using Connected.Entities;
using Connected.Identities.Authentication.Dtos;
using Connected.Services;
using Connected.Storage;
using System.Collections.Immutable;

namespace Connected.Identities.Authentication.Ops;

internal class Query(IStorageProvider storage)
  : ServiceFunction<IQueryIdentityAuthenticationTokensDto, IImmutableList<IIdentityAuthenticationToken>>
{
	protected override async Task<IImmutableList<IIdentityAuthenticationToken>> OnInvoke()
	{
		return await storage.Open<IdentityAuthenticationToken>().AsEntities<IIdentityAuthenticationToken>(f =>
						(Dto.Identity is null || string.Equals(f.Identity, Dto.Identity, StringComparison.Ordinal))
					&& (Dto.Key is null || string.Equals(f.Key, Dto.Key, StringComparison.Ordinal)));
	}
}
