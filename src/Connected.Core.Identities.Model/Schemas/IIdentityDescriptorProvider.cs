using Connected.Identities;
using Connected.Services;
using System.Collections.Immutable;

namespace Connected.Membership.Schemas;

public interface IIdentityDescriptorProvider : IMiddleware
{
	string Name { get; }

	Task<IImmutableList<IIdentityDescriptor>> Query();
	Task<IImmutableList<IIdentityDescriptor>> Query(IValueListDto<string> dto);
	/*
	 * Query Dependencies, i.e. users that belong to a specified role.
	 */
	Task<IImmutableList<IIdentityDescriptor>> Query(IValueDto<string> dto);

	Task<IIdentityDescriptor?> Select(IValueDto<string> dto);
}