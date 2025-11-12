using Connected.Entities;
using Connected.Identities.Dtos;
using Connected.Services.Validation;

namespace Connected.Identities.Users.Validation;

internal sealed class InsertUserValidator(IUserCache cache)
	: Validator<IInsertUserDto>
{
	protected override async Task OnInvoke()
	{
		if (Dto.Email is not null)
		{
			var existing = await cache.AsEntity(f => string.Equals(f.Email, Dto.Email, StringComparison.OrdinalIgnoreCase));

			if (existing is not null)
				throw ValidationExceptions.ValueExists(nameof(Dto.Email), Dto.Email);
		}
	}
}
