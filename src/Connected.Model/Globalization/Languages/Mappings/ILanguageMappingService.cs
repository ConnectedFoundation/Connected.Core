using Connected.Annotations;
using Connected.Globalization.Languages.Mappings.Dtos;
using Connected.Services;
using System.Collections.Immutable;

namespace Connected.Globalization.Languages.Mappings;
/// <summary>
/// Service for managing language mappings between languages and external identifiers.
/// </summary>
[Service, ServiceUrl(Urls.LanguageMappingService)]
public interface ILanguageMappingService
{
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
	Task<IImmutableList<ILanguageMapping>> Query(IQueryLanguageMappingDto dto);
}
