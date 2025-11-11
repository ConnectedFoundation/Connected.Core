using Connected.Annotations;
using Connected.Identities.Globalization.Dtos;
using Connected.Services;
using System.Collections.Immutable;

namespace Connected.Identities.Globalization;

/// <summary>
/// Provides service operations for managing identity globalization settings.
/// </summary>
/// <remarks>
/// This service interface defines the contract for CRUD operations on identity
/// globalization preferences, including insert, update, delete, and query operations.
/// All methods are asynchronous and support various selection criteria for flexible
/// globalization settings management.
/// </remarks>
[Service, ServiceUrl(IdentitiesUrls.GlobalizationService)]
public interface IIdentityGlobalizationService
{
	/// <summary>
	/// Asynchronously inserts new identity globalization settings.
	/// </summary>
	/// <param name="dto">The data transfer object containing the globalization settings to insert.</param>
	/// <returns>A task that represents the asynchronous operation.</returns>
	[ServiceOperation(ServiceOperationVerbs.Post)]
	Task Insert(IInsertIdentityGlobalizationDto dto);

	/// <summary>
	/// Asynchronously updates existing identity globalization settings.
	/// </summary>
	/// <param name="dto">The data transfer object containing the updated globalization settings.</param>
	/// <returns>A task that represents the asynchronous operation.</returns>
	[ServiceOperation(ServiceOperationVerbs.Put)]
	Task Update(IUpdateIdentityGlobalizationDto dto);

	/// <summary>
	/// Asynchronously deletes identity globalization settings by primary key.
	/// </summary>
	/// <param name="dto">The data transfer object containing the primary key of the globalization settings to delete.</param>
	/// <returns>A task that represents the asynchronous operation.</returns>
	[ServiceOperation(ServiceOperationVerbs.Delete)]
	Task Delete(IPrimaryKeyDto<string> dto);

	/// <summary>
	/// Asynchronously queries identity globalization settings based on specified criteria.
	/// </summary>
	/// <param name="dto">The data transfer object containing the query criteria, or null to retrieve all settings.</param>
	/// <returns>A task that represents the asynchronous operation. The task result contains an immutable list of identity globalization settings matching the query criteria.</returns>
	[ServiceOperation(ServiceOperationVerbs.Get)]
	Task<IImmutableList<IIdentityGlobalization>> Query(IQueryDto? dto);

	/// <summary>
	/// Asynchronously selects a single identity globalization settings entity by its primary key.
	/// </summary>
	/// <param name="dto">The data transfer object containing the primary key of the globalization settings to select.</param>
	/// <returns>A task that represents the asynchronous operation. The task result contains the identity globalization settings if found, or null otherwise.</returns>
	[ServiceOperation(ServiceOperationVerbs.Get)]
	Task<IIdentityGlobalization?> Select(IPrimaryKeyDto<string> dto);
}