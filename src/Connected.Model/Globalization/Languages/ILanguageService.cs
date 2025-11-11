using Connected.Annotations;
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
	/// Asynchronously inserts a new language.
	/// </summary>
	/// <param name="dto">DTO containing language data.</param>
	/// <returns>The primary key of the newly created language.</returns>
	[ServiceOperation(ServiceOperationVerbs.Post | ServiceOperationVerbs.Put)]
	Task<int> Insert(IInsertLanguageDto dto);
	/// <summary>
	/// Asynchronously updates an existing language.
	/// </summary>
	/// <param name="dto">DTO containing updated language data.</param>
	[ServiceOperation(ServiceOperationVerbs.Post)]
	Task Update(IUpdateLanguageDto dto);
	/// <summary>
	/// Asynchronously deletes a language by its primary key.
	/// </summary>
	/// <param name="dto">DTO containing the primary key of the language to delete.</param>
	[ServiceOperation(ServiceOperationVerbs.Post | ServiceOperationVerbs.Delete)]
	Task Delete(IPrimaryKeyDto<int> dto);
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
