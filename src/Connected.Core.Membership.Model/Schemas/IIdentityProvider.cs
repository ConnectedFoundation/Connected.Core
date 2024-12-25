using Connected.Identities;
using System.Collections.Immutable;

namespace Connected.Membership.Schemas;

public interface IIdentityProvider : IMiddleware
{
	string Name { get; }

	Task<ImmutableList<IIdentity>> Query();
	Task<IIdentity?> Select(string key);
	Task<ImmutableList<IIdentity>> QueryDependencies(string key);
}