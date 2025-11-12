using Connected.Services;
using System.Collections.Immutable;

namespace Connected.Identities.Schemas;
internal sealed class UserIdentityProvider(IUserService users)
	: IdentityDescriptorProvider
{
	public override string Name => "Users";

	protected override async Task<IImmutableList<IIdentityDescriptor>> OnQuery()
	{
		return AsIdentityDescriptors(await users.Query(QueryDto.NoPaging));
	}

	protected override async Task<IImmutableList<IIdentityDescriptor>> OnQuery(IValueListDto<string> dto)
	{
		return AsIdentityDescriptors(await users.Query(dto));
	}

	protected override async Task<IIdentityDescriptor?> OnSelect(IValueDto<string> dto)
	{
		return AsIdentityDescriptor(await users.Select(dto));
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