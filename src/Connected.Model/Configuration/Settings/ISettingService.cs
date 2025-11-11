using Connected.Annotations;
using Connected.Services;
using System.Collections.Immutable;

namespace Connected.Configuration.Settings;
// <summary>
// Service contract for querying, updating, and deleting configuration settings.
// </summary>
// <remarks>
// Provides CRUD-style methods plus name-based selection. Operations are exposed via service operation
// attributes mapping to verbs and optional custom URLs.
// </remarks>
[Service]
[ServiceUrl(Urls.Settings)]
public interface ISettingService
{
	/// <summary>
	/// Selects a setting by its primary key.
	/// </summary>
	/// <param name="dto">DTO containing integer primary key.</param>
	/// <returns>The setting or null if not found.</returns>
	[ServiceOperation(ServiceOperationVerbs.Get | ServiceOperationVerbs.Post)]
	Task<ISetting?> Select(IPrimaryKeyDto<int> dto);
	/// <summary>
	/// Selects a setting by its unique name.
	/// </summary>
	/// <param name="dto">DTO containing the setting name.</param>
	/// <returns>The setting or null if not found.</returns>
	[ServiceOperation(ServiceOperationVerbs.Get | ServiceOperationVerbs.Post)]
	[ServiceUrl("select-by-name")]
	Task<ISetting?> Select(INameDto dto);
	/// <summary>
	/// Queries settings using optional filtering criteria.
	/// </summary>
	/// <param name="dto">Optional query DTO; null returns all settings.</param>
	/// <returns>List of settings matching criteria.</returns>
	[ServiceOperation(ServiceOperationVerbs.Get)]
	Task<IImmutableList<ISetting>> Query(IQueryDto? dto);
	/// <summary>
	/// Updates or creates a setting value.
	/// </summary>
	/// <param name="dto">DTO specifying name and value.</param>
	[ServiceOperation(ServiceOperationVerbs.Post | ServiceOperationVerbs.Patch)]
	Task Update(IUpdateSettingDto dto);
	/// <summary>
	/// Deletes a setting by its primary key.
	/// </summary>
	/// <param name="dto">DTO containing primary key.</param>
	[ServiceOperation(ServiceOperationVerbs.Post | ServiceOperationVerbs.Delete)]
	Task Delete(IPrimaryKeyDto<int> dto);
}
