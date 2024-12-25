using Connected.Annotations;
using Connected.Services;

namespace Connected.Globalization.Languages;

[Service, ServiceUrl(Urls.LanguageMappingService)]
public interface ILanguageMappingService
{
	[ServiceOperation(ServiceOperationVerbs.Post | ServiceOperationVerbs.Put)]
	Task<int> Insert(IInsertLanguageMappingDto dto);

	[ServiceOperation(ServiceOperationVerbs.Post)]
	Task Update(IUpdateLanguageMappingDto dto);

	[ServiceOperation(ServiceOperationVerbs.Post | ServiceOperationVerbs.Delete)]
	Task Delete(IPrimaryKeyDto<int> dto);

	[ServiceOperation(ServiceOperationVerbs.Post | ServiceOperationVerbs.Get)]
	Task<ILanguageMapping?> Select(IPrimaryKeyDto<int> dto);

	[ServiceOperation(ServiceOperationVerbs.Post | ServiceOperationVerbs.Get)]
	Task<ImmutableList<ILanguageMapping>> Query(IQueryLanguageMappingsDto dto);
}
