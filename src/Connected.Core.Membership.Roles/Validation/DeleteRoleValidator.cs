using Connected.Annotations;
using Connected.Services;
using Connected.Validation;
using System.ComponentModel.DataAnnotations;

namespace Connected.Membership.Roles.Validation;

[Middleware<IRoleService>(nameof(IRoleService.Delete))]
internal sealed class DeleteRoleValidator
	: Validator<IPrimaryKeyDto<int>>
{
	protected override async Task OnInvoke()
	{
		if (RoleUtils.IsVirtual(Dto.Id))
			throw new ValidationException(SR.ValVirtualRoleDelete);

		await Task.CompletedTask;
	}
}
