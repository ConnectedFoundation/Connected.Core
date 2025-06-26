using Connected.Annotations;
using Connected.Membership.Roles.Dtos;
using Connected.Validation;
using System.ComponentModel.DataAnnotations;

namespace Connected.Membership.Roles.Validation;

[Middleware<IRoleService>(nameof(IRoleService.Update))]
internal sealed class UpdateRoleValidator
	: Validator<IUpdateRoleDto>
{
	protected override async Task OnInvoke()
	{
		if (RoleUtils.IsVirtual(Dto.Id))
			throw new ValidationException(SR.ValVirtualRoleDelete);

		await Task.CompletedTask;
	}
}
