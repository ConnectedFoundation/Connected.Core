using Connected.Identities;
using Connected.Membership.Dtos;
using Connected.Membership.Roles;
using Connected.Services;
using System.Collections.Immutable;

namespace Connected.Membership.Ops;

internal sealed class Query(IRoleService roles, IMembershipService membership, IUserService users)
	: ServiceFunction<IQueryUserDto, IImmutableList<string>>
{
	protected override async Task<IImmutableList<string>> OnInvoke()
	{
		var dto = new Dto<IValueDto<string>>().Value;

		dto.Value = Dto.Role;

		var role = await roles.Select(dto);

		if (role is null)
			return [];

		var result = new HashSet<string>();
		var references = new HashSet<string> { role.Token };

		await Discover(role, result, references);

		return result.ToImmutableList();
	}

	private async Task Discover(IRole role, HashSet<string> identities, HashSet<string> references)
	{
		if (references.Contains(role.Token))
			return;

		references.Add(role.Token);

		var queryDto = new Dto<IQueryMembershipDto>().Value;

		queryDto.Role = role.Id;

		var membershipEntities = await membership.Query(queryDto);

		foreach (var item in membershipEntities)
		{
			if (await IsUser(item.Identity))
				identities.Add(item.Identity);
			else if (await IsRole(item.Identity))
			{
				var membershipDto = new Dto<IValueDto<string>>().Value;

				membershipDto.Value = item.Identity;

				var membershipRole = await roles.Select(membershipDto);

				if (membershipRole is not null)
					await Discover(membershipRole, identities, references);
			}
		}

		if (role.Parent.HasValue)
		{
			var parentRole = await roles.Select(new PrimaryKeyDto<int> { Id = role.Parent.Value });

			if (parentRole is not null)
				await Discover(parentRole, identities, references);
		}
	}

	private async Task<bool> IsRole(string identity)
	{
		var dto = new Dto<IValueDto<string>>().Value;

		dto.Value = identity;

		return await roles.Select(dto) is not null;
	}

	private async Task<bool> IsUser(string identity)
	{
		var dto = new Dto<IValueDto<string>>().Value;

		dto.Value = identity;

		return await users.Select(dto) is not null;
	}
}
