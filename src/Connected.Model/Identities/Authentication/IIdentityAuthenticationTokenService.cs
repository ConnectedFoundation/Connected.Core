using Connected.Annotations;
using Connected.Identities.Authentication.Dtos;
using Connected.Services;
using System.Collections.Immutable;

namespace Connected.Identities.Authentication;

/// <summary>
/// Provides service operations for managing identity authentication tokens.
/// </summary>
/// <remarks>
/// This service interface defines the contract for CRUD operations on authentication tokens,
/// including insert, update, delete, and query operations. All methods are asynchronous
/// and support various selection criteria for flexible token management.
/// </remarks>
[Service]
public interface IIdentityAuthenticationTokenService
{
	/// <summary>
	/// Asynchronously inserts a new authentication token.
	/// </summary>
	/// <param name="dto">The data transfer object containing the token information to insert.</param>
	/// <returns>A task that represents the asynchronous operation. The task result contains the identifier of the newly created token, or null if the operation failed.</returns>
	[ServiceOperation(ServiceOperationVerbs.Post)]
	Task<long?> Insert(IInsertIdentityAuthenticationTokenDto dto);

	/// <summary>
	/// Asynchronously updates an existing authentication token.
	/// </summary>
	/// <param name="dto">The data transfer object containing the updated token information.</param>
	/// <returns>A task that represents the asynchronous operation.</returns>
	[ServiceOperation(ServiceOperationVerbs.Put)]
	Task Update(IUpdateIdentityAuthenticationTokenDto dto);

	/// <summary>
	/// Asynchronously deletes an authentication token by its primary key.
	/// </summary>
	/// <param name="dto">The data transfer object containing the primary key of the token to delete.</param>
	/// <returns>A task that represents the asynchronous operation.</returns>
	[ServiceOperation(ServiceOperationVerbs.Delete)]
	Task Delete(IPrimaryKeyDto<long> dto);

	/// <summary>
	/// Asynchronously queries authentication tokens based on specified criteria.
	/// </summary>
	/// <param name="dto">The data transfer object containing the query criteria.</param>
	/// <returns>A task that represents the asynchronous operation. The task result contains an immutable list of authentication tokens matching the query criteria.</returns>
	[ServiceOperation(ServiceOperationVerbs.Get)]
	Task<IImmutableList<IIdentityAuthenticationToken>> Query(IQueryIdentityAuthenticationTokensDto dto);

	/// <summary>
	/// Asynchronously selects a single authentication token by its primary key.
	/// </summary>
	/// <param name="dto">The data transfer object containing the primary key of the token to select.</param>
	/// <returns>A task that represents the asynchronous operation. The task result contains the authentication token if found, or null otherwise.</returns>
	[ServiceOperation(ServiceOperationVerbs.Get)]
	Task<IIdentityAuthenticationToken?> Select(IPrimaryKeyDto<long> dto);

	/// <summary>
	/// Asynchronously selects a single authentication token by a string value.
	/// </summary>
	/// <param name="dto">The data transfer object containing the string value to search for.</param>
	/// <returns>A task that represents the asynchronous operation. The task result contains the authentication token if found, or null otherwise.</returns>
	[ServiceOperation(ServiceOperationVerbs.Get)]
	Task<IIdentityAuthenticationToken?> Select(IValueDto<string> dto);
}