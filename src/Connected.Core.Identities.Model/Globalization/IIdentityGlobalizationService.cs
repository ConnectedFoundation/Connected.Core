using Connected.Annotations;
using Connected.Services;

namespace Connected.Identities.Globalization;

[Service, ServiceUrl(Urls.GlobalizationService)]
public interface IIdentityGlobalizationService
{
	[ServiceOperation(ServiceOperationVerbs.Put | ServiceOperationVerbs.Post)]
	Task Insert(IInsertIdentityGlobalizationDto dto);

	[ServiceOperation(ServiceOperationVerbs.Post)]
	Task Update(IUpdateIdentityGlobalizationDto dto);

	[ServiceOperation(ServiceOperationVerbs.Delete | ServiceOperationVerbs.Post)]
	Task Delete(IPrimaryKeyDto<string> dto);

	[ServiceOperation(ServiceOperationVerbs.Get | ServiceOperationVerbs.Post)]
	Task<ImmutableList<IIdentityGlobalization>> Query(IQueryDto? dto);

	[ServiceOperation(ServiceOperationVerbs.Get | ServiceOperationVerbs.Post)]
	Task<IIdentityGlobalization?> Select(IPrimaryKeyDto<string> dto);
}