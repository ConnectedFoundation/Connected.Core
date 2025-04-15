using Connected.Identities;
using System.Collections.Immutable;

namespace Connected.Membership.Schemas;

public interface IIdentityProvider : IMiddleware
{
	string Name { get; }

	Task<IImmutableList<IIdentity>> Query();
	Task<IIdentity?> Select(string key);
	Task<IImmutableList<IIdentity>> QueryDependencies(string key);
}