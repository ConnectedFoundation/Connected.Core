using Connected.Identities;
using Connected.Identities.Schemas;
using Connected.Membership.Roles;
using Connected.Services;
using System.Collections.Immutable;

namespace Connected.Membership.Schemas;
internal sealed class RoleIdentityProvider(IUserService users, IRoleService roles)
	: IdentityDescriptorProvider
{
	public override string Name => "Roles";

	protected override async Task<IImmutableList<IIdentityDescriptor>> OnQuery()
	{
		return AsIdentityDescriptors(await roles.Query(QueryDto.NoPaging));
	}

	protected override async Task<IImmutableList<IIdentityDescriptor>> OnQuery(IValueListDto<string> dto)
	{
		return AsIdentityDescriptors(await users.Query(dto));
	}

	protected override async Task<IIdentityDescriptor?> OnSelect(IValueDto<string> dto)
	{
		return AsIdentityDescriptor(await roles.Select(dto));
	}

	private static IdentityDescriptor? AsIdentityDescriptor(IRole? role)
	{
		if (role is null)
			return null;

		return new IdentityDescriptor
		{
			Name = role.Name,
			Token = role.Token
		};
	}
	private static IdentityDescriptor? AsIdentityDescriptor(IUser? user)
	{
		if (user is null)
			return null;

		return new IdentityDescriptor
		{
			Name = $"{user.FirstName} {user.LastName}",
			Token = user.Token
		};
	}

	private static IImmutableList<IIdentityDescriptor> AsIdentityDescriptors(IImmutableList<IRole> items)
	{
		var result = new List<IIdentityDescriptor>();

		foreach (var item in items)
		{
			if (AsIdentityDescriptor(item) is IIdentityDescriptor descriptor)
				result.Add(descriptor);
		}

		return [.. result];
	}

	private static IImmutableList<IIdentityDescriptor> AsIdentityDescriptors(IImmutableList<IUser> items)
	{
		var result = new List<IIdentityDescriptor>();

		foreach (var item in items)
		{
			if (AsIdentityDescriptor(item) is IIdentityDescriptor descriptor)
				result.Add(descriptor);
		}

		return [.. result];
	}
}
