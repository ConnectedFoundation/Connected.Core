using Connected.Annotations;
using Connected.Membership.Claims.Dtos;
using Connected.Services;

namespace Connected.Membership.Claims;

[ServiceRegistration(ServiceRegistrationMode.Manual)]
internal sealed class QueryClaimAmbient : AmbientProvider<IQueryClaimDto>, IQueryClaimAmbient
{
	protected override async Task OnInvoke()
	{
		Dto.Entity ??= IClaim.UndefinedEntity;
		Dto.EntityId ??= IClaim.UndefinedId;

		await Task.CompletedTask;
	}
}
