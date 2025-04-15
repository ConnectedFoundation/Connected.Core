using Connected.Identities;
using System.Collections.Immutable;

namespace Connected.Membership.Schemas;

public abstract class IdentityProvider : Middleware, IIdentityProvider
{
	public abstract string Name { get; }

	public async Task<IImmutableList<IIdentity>> Query()
	{
		return await OnQuery();
	}

	public async Task<IIdentity?> Select(string key)
	{
		return await OnSelect(key);
	}

	public async Task<IImmutableList<IIdentity>> QueryDependencies(string key)
	{
		return await OnQueryDependencies(key);
	}

	protected virtual Task<IImmutableList<IIdentity>> OnQuery()
	{
		return Task.FromResult<IImmutableList<IIdentity>>(ImmutableList<IIdentity>.Empty);
	}

	protected virtual Task<IIdentity?> OnSelect(string key)
	{
		return Task.FromResult<IIdentity?>(null);
	}

	protected virtual Task<IImmutableList<IIdentity>> OnQueryDependencies(string key)
	{
		return Task.FromResult<IImmutableList<IIdentity>>(ImmutableList<IIdentity>.Empty);
	}
}