using Connected.Annotations;
using Connected.Globalization.Languages.Dtos;
using Connected.Services;
using System.Collections.Immutable;

namespace Connected.Globalization.Languages;
/// <summary>
/// Service for managing language configuration including creation, updates, and queries.
/// </summary>
[Service, ServiceUrl(Urls.LanguageService)]
public interface ILanguageService
{
	/// <summary>
	/// Asynchronously selects a language by its primary key.
	/// </summary>
	/// <param name="dto">DTO containing the primary key.</param>
	/// <returns>The language if found; otherwise null.</returns>
	[ServiceOperation(ServiceOperationVerbs.Post | ServiceOperationVerbs.Get)]
	Task<ILanguage?> Select(IPrimaryKeyDto<int> dto);
	/// <summary>
	/// Asynchronously selects a language by its LCID.
	/// </summary>
	/// <param name="dto">DTO containing the LCID.</param>
	/// <returns>The language if found; otherwise null.</returns>
	[ServiceOperation(ServiceOperationVerbs.Post | ServiceOperationVerbs.Get)]
	[ServiceUrl("select-by-lcid")]
	Task<ILanguage?> Select(ISelectLanguageDto dto);
	/// <summary>
	/// Asynchronously queries languages using optional filtering criteria.
	/// </summary>
	/// <param name="dto">Optional DTO containing query criteria; null returns all languages.</param>
	/// <returns>A list of languages matching the criteria.</returns>
	[ServiceOperation(ServiceOperationVerbs.Post | ServiceOperationVerbs.Get)]
	Task<IImmutableList<ILanguage>> Query(IQueryDto? dto);
}
