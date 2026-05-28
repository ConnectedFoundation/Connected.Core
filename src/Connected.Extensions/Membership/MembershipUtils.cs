using Connected.Membership.Dtos;
using Connected.Membership.Roles;
using Connected.Services;
using System.Collections.Immutable;

namespace Connected.Membership;

public static class MembershipUtils
{
	public static async Task<bool> IsMemberOf(string identity, string membershipIdentity, IMembershipService membership, IRoleService roles)
	{
		if (string.IsNullOrWhiteSpace(identity) || string.IsNullOrWhiteSpace(membershipIdentity))
			return false;

		if (string.Equals(identity, membershipIdentity, StringComparison.OrdinalIgnoreCase))
			return true;

		var identityTokens = await ResolveIdentityTokens(identity, membership, roles);
		var membershipTokens = await ResolveIdentityTokens(membershipIdentity, membership, roles);

		return identityTokens.Any(token => membershipTokens.Any(target => string.Equals(token, target, StringComparison.OrdinalIgnoreCase)));
	}

	public static async Task<ImmutableList<string>> ResolveIdentityTokens(string? identity, IMembershipService membership, IRoleService roles)
	{
		var result = new List<string>();

		if (identity is null)
			return [.. result];

		AddToken(result, identity);

		var ownRole = await roles.Select(Dto.Factory.CreateValue(identity));

		var references = new List<int>();

		if (ownRole is not null)
			await ProcessRole(roles, ownRole.Id, result, references);

		var dto = Dto.Factory.Create<IQueryMembershipDto>();

		dto.Identity = identity;

		var items = await membership.Query(dto);

		foreach (var item in items)
			await ProcessRole(roles, item.Role, result, references);

		return [.. result];
	}

	private static void AddToken(List<string> items, string token)
	{
		if (!items.Any(f => string.Equals(f, token, StringComparison.OrdinalIgnoreCase)))
			items.Add(token);
	}

	private static async Task ProcessRole(IRoleService roles, int roleId, List<string> items, List<int> references)
	{
		if (references.Contains(roleId))
			return;

		references.Add(roleId);

		var role = await roles.Select(Dto.Factory.CreatePrimaryKey(roleId));

		if (role is null)
			return;

		AddToken(items, role.Token);

		if (role.Parent is not null)
			await ProcessRole(roles, role.Parent.GetValueOrDefault(), items, references);
	}
}
