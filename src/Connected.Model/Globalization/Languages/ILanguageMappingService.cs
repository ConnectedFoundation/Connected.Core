using Connected.Annotations;
using Connected.Services;
using System.Collections.Immutable;

namespace Connected.Globalization.Languages;
/// <summary>
/// Service for managing language mappings between languages and external identifiers.
/// </summary>
[Service, ServiceUrl(Urls.LanguageMappingService)]
public interface ILanguageMappingService
{
	/// <summary>
	/// Asynchronously inserts a new language mapping.
	/// </summary>
	/// <param name="dto">DTO containing language mapping data.</param>
	/// <returns>The primary key of the newly created language mapping.</returns>
	[ServiceOperation(ServiceOperationVerbs.Post)]
	Task<int> Insert(IInsertLanguageMappingDto dto);
	/// <summary>
	/// Asynchronously updates an existing language mapping.
	/// </summary>
	/// <param name="dto">DTO containing updated language mapping data.</param>
	[ServiceOperation(ServiceOperationVerbs.Put)]
	Task Update(IUpdateLanguageMappingDto dto);
	/// <summary>
	/// Asynchronously deletes a language mapping by its primary key.
	/// </summary>
	/// <param name="dto">DTO containing the primary key of the mapping to delete.</param>
	[ServiceOperation(ServiceOperationVerbs.Delete)]
	Task Delete(IPrimaryKeyDto<int> dto);
	/// <summary>
	/// Asynchronously selects a language mapping by its primary key.
	/// </summary>
	/// <param name="dto">DTO containing the primary key.</param>
	/// <returns>The language mapping if found; otherwise null.</returns>
	[ServiceOperation(ServiceOperationVerbs.Get)]
	Task<ILanguageMapping?> Select(IPrimaryKeyDto<int> dto);
	/// <summary>
	/// Asynchronously queries language mappings using optional filtering criteria.
	/// </summary>
	/// <param name="dto">DTO containing query criteria.</param>
	/// <returns>A list of language mappings matching the criteria.</returns>
	[ServiceOperation(ServiceOperationVerbs.Get)]
	Task<IImmutableList<ILanguageMapping>> Query(IQueryLanguageMappingsDto dto);
}
