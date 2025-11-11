using Connected.Annotations;
using Connected.Identities.MetaData.Dtos;
using Connected.Services;
using System.Collections.Immutable;

namespace Connected.Identities.MetaData;

/// <summary>
/// Provides service operations for managing identity metadata.
/// </summary>
/// <remarks>
/// This service interface defines the contract for CRUD operations on identity metadata,
/// including insert, update, delete, and query operations. All methods are asynchronous
/// and support various selection criteria for flexible metadata management.
/// </remarks>
[Service, ServiceUrl(IdentitiesUrls.MetaDataService)]
public interface IIdentityMetaDataService
{
	/// <summary>
	/// Asynchronously inserts new identity metadata.
	/// </summary>
	/// <param name="dto">The data transfer object containing the metadata information to insert.</param>
	/// <returns>A task that represents the asynchronous operation.</returns>
	[ServiceOperation(ServiceOperationVerbs.Post)]
	Task Insert(IInsertIdentityMetaDataDto dto);

	/// <summary>
	/// Asynchronously updates existing identity metadata.
	/// </summary>
	/// <param name="dto">The data transfer object containing the updated metadata information.</param>
	/// <returns>A task that represents the asynchronous operation.</returns>
	[ServiceOperation(ServiceOperationVerbs.Put)]
	Task Update(IUpdateIdentityMetaDataDto dto);

	/// <summary>
	/// Asynchronously deletes identity metadata by its primary key.
	/// </summary>
	/// <param name="dto">The data transfer object containing the primary key of the metadata to delete.</param>
	/// <returns>A task that represents the asynchronous operation.</returns>
	[ServiceOperation(ServiceOperationVerbs.Delete)]
	Task Delete(IPrimaryKeyDto<string> dto);

	/// <summary>
	/// Asynchronously queries identity metadata based on specified criteria.
	/// </summary>
	/// <param name="dto">The data transfer object containing the query criteria, or null to retrieve all metadata.</param>
	/// <returns>A task that represents the asynchronous operation. The task result contains an immutable list of identity metadata matching the query criteria.</returns>
	[ServiceOperation(ServiceOperationVerbs.Get)]
	Task<IImmutableList<IIdentityMetaData>> Query(IQueryDto? dto);

	/// <summary>
	/// Asynchronously queries identity metadata by a list of primary keys.
	/// </summary>
	/// <param name="dto">The data transfer object containing the list of primary keys to query.</param>
	/// <returns>A task that represents the asynchronous operation. The task result contains an immutable list of identity metadata matching the specified primary keys.</returns>
	[ServiceOperation(ServiceOperationVerbs.Get)]
	Task<IImmutableList<IIdentityMetaData>> Query(IPrimaryKeyListDto<string> dto);

	/// <summary>
	/// Asynchronously selects a single identity metadata entity by its primary key.
	/// </summary>
	/// <param name="dto">The data transfer object containing the primary key of the metadata to select.</param>
	/// <returns>A task that represents the asynchronous operation. The task result contains the identity metadata if found, or null otherwise.</returns>
	[ServiceOperation(ServiceOperationVerbs.Get)]
	Task<IIdentityMetaData?> Select(IPrimaryKeyDto<string> dto);
}