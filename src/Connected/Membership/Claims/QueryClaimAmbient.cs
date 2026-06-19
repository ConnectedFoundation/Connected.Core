using Connected.Annotations;
using Connected.Membership.Claims.Dtos;
using Connected.Services;

namespace Connected.Membership.Claims;

[ServiceRegistration(ServiceRegistrationMode.Manual)]
internal sealed class QueryClaimAmbient : AmbientProvider<IQueryClaimDto>, IQueryClaimAmbient
{
	protected override async Task OnInvoke()
	{
		if (!Dto.Entities.Any())
			Dto.Entities = [IClaim.UndefinedEntity];

		if (!Dto.EntityIds.Any())
			Dto.EntityIds = [IClaim.UndefinedId];

		await Task.CompletedTask;
	}
}
