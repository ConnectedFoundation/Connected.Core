using Connected.Membership.Dtos;
using Connected.Membership.Roles;
using Connected.Services;
using System.Collections.Immutable;

namespace Connected.Membership;

public static class MembershipUtils
{
	public static async Task<ImmutableList<string>> ResolveIdentityTokens(string? identity, IMembershipService membership, IRoleService roles)
	{
		var result = new List<string>();

		if (identity is null)
			return [.. result];

		result.Add(identity);

		var dto = Dto.Factory.Create<IQueryMembershipDto>();

		dto.Identity = identity;

		var items = await membership.Query(dto);

		foreach (var item in items)
			await ProcessRole(roles, item.Role, result);

		return [.. result];
	}

	private static async Task ProcessRole(IRoleService roles, int roleId, List<string> items)
	{
		var role = await roles.Select(Dto.Factory.CreatePrimaryKey(roleId));

		if (role is null)
			return;

		if (!items.Contains(role.Token))
			items.Add(role.Token);

		if (role.Parent is not null)
			await ProcessRole(roles, role.Parent.GetValueOrDefault(), items);
	}
}
