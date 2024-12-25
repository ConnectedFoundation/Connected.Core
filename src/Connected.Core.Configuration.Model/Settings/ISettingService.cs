using Connected.Annotations;
using Connected.Services;

namespace Connected.Configuration.Settings;

[Service]
[ServiceUrl(Urls.Settings)]
public interface ISettingService
{
	[ServiceOperation(ServiceOperationVerbs.Get | ServiceOperationVerbs.Post)]
	Task<ISetting?> Select(IPrimaryKeyDto<int> dto);

	[ServiceOperation(ServiceOperationVerbs.Get | ServiceOperationVerbs.Post)]
	[ServiceUrl("select-by-name")]
	Task<ISetting?> Select(INameDto dto);

	[ServiceOperation(ServiceOperationVerbs.Get)]
	Task<ImmutableList<ISetting>> Query(IQueryDto? dto);

	[ServiceOperation(ServiceOperationVerbs.Post | ServiceOperationVerbs.Patch)]
	Task Update(ISettingDto dto);

	[ServiceOperation(ServiceOperationVerbs.Post | ServiceOperationVerbs.Delete)]
	Task Delete(IPrimaryKeyDto<int> dto);
}
