using Connected.Annotations;
using Connected.Services;

namespace Connected.Globalization.Languages;

[Service, ServiceUrl(Urls.LanguageService)]
public interface ILanguageService
{
	[ServiceOperation(ServiceOperationVerbs.Post | ServiceOperationVerbs.Put)]
	Task<int> Insert(IInsertLanguageDto dto);

	[ServiceOperation(ServiceOperationVerbs.Post)]
	Task Update(IUpdateLanguageDto dto);

	[ServiceOperation(ServiceOperationVerbs.Post | ServiceOperationVerbs.Delete)]
	Task Delete(IPrimaryKeyDto<int> dto);

	[ServiceOperation(ServiceOperationVerbs.Post | ServiceOperationVerbs.Get)]
	Task<ILanguage?> Select(IPrimaryKeyDto<int> dto);

	[ServiceOperation(ServiceOperationVerbs.Post | ServiceOperationVerbs.Get)]
	[ServiceUrl("select-by-lcid")]
	Task<ILanguage?> Select(ISelectLanguageDto dto);

	[ServiceOperation(ServiceOperationVerbs.Post | ServiceOperationVerbs.Get)]
	Task<ImmutableList<ILanguage>> Query(IQueryDto? dto);
}
