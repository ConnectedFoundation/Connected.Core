using Connected.Identities;

namespace Connected.Membership.Schemas;

public abstract class IdentityProvider : MiddlewareComponent, IIdentityProvider
{
	public abstract string Name { get; }

	public async Task<ImmutableList<IIdentity>> Query()
	{
		return await OnQuery();
	}

	public async Task<IIdentity?> Select(string key)
	{
		return await OnSelect(key);
	}

	public async Task<ImmutableList<IIdentity>> QueryDependencies(string key)
	{
		return await OnQueryDependencies(key);
	}

	protected virtual Task<ImmutableList<IIdentity>> OnQuery()
	{
		return Task.FromResult(ImmutableList<IIdentity>.Empty);
	}

	protected virtual Task<IIdentity?> OnSelect(string key)
	{
		return Task.FromResult<IIdentity?>(null);
	}

	protected virtual Task<ImmutableList<IIdentity>> OnQueryDependencies(string key)
	{
		return Task.FromResult(ImmutableList<IIdentity>.Empty);
	}
}