using Connected.Annotations;
using Connected.Identities.Dtos;
using Connected.Services;
using System.Collections.Immutable;

namespace Connected.Identities;

/// <summary>
/// Provides service operations for managing users.
/// </summary>
/// <remarks>
/// This service interface defines the contract for comprehensive user management operations,
/// including CRUD operations, credential-based selection, token-based lookups, and password
/// updates. All methods are asynchronous and support various selection and query criteria
/// for flexible user management.
/// </remarks>
[Service, ServiceUrl(IdentitiesUrls.Users)]
public interface IUserService
{
	/// <summary>
	/// Asynchronously queries users based on specified criteria.
	/// </summary>
	/// <param name="dto">The data transfer object containing the query criteria, or null to retrieve all users.</param>
	/// <returns>A task that represents the asynchronous operation. The task result contains an immutable list of users matching the query criteria.</returns>
	[ServiceOperation(ServiceOperationVerbs.Get)]
	Task<IImmutableList<IUser>> Query(IQueryDto? dto);

	/// <summary>
	/// Asynchronously queries users by a list of primary keys for lookup operations.
	/// </summary>
	/// <param name="dto">The data transfer object containing the list of primary keys to query.</param>
	/// <returns>A task that represents the asynchronous operation. The task result contains an immutable list of users matching the specified primary keys.</returns>
	[ServiceOperation(ServiceOperationVerbs.Get), ServiceUrl(IdentitiesUrls.LookupOperation)]
	Task<IImmutableList<IUser>> Query(IPrimaryKeyListDto<int> dto);

	/// <summary>
	/// Asynchronously queries users by a list of tokens for lookup operations.
	/// </summary>
	/// <param name="dto">The data transfer object containing the list of tokens to query.</param>
	/// <returns>A task that represents the asynchronous operation. The task result contains an immutable list of users matching the specified tokens.</returns>
	[ServiceOperation(ServiceOperationVerbs.Get), ServiceUrl(IdentitiesUrls.LookupByTokenOperation)]
	Task<IImmutableList<IUser>> Query(IValueListDto<string> dto);

	/// <summary>
	/// Asynchronously selects a single user by its primary key.
	/// </summary>
	/// <param name="dto">The data transfer object containing the primary key of the user to select.</param>
	/// <returns>A task that represents the asynchronous operation. The task result contains the user if found, or null otherwise.</returns>
	[ServiceOperation(ServiceOperationVerbs.Get)]
	Task<IUser?> Select(IPrimaryKeyDto<long> dto);

	/// <summary>
	/// Asynchronously selects a single user by credentials.
	/// </summary>
	/// <param name="dto">The data transfer object containing the credentials for user selection.</param>
	/// <returns>A task that represents the asynchronous operation. The task result contains the user if credentials are valid, or null otherwise.</returns>
	[ServiceOperation(ServiceOperationVerbs.Get), ServiceUrl(IdentitiesUrls.SelectByCredentialsOperation)]
	Task<IUser?> Select(ISelectUserDto dto);

	/// <summary>
	/// Asynchronously resolves a user by a token value.
	/// </summary>
	/// <param name="dto">The data transfer object containing the token value to resolve.</param>
	/// <returns>A task that represents the asynchronous operation. The task result contains the user if found, or null otherwise.</returns>
	[ServiceOperation(ServiceOperationVerbs.Get), ServiceUrl(IdentitiesUrls.ResolveOperation)]
	Task<IUser?> Select(IValueDto<string> dto);

	/// <summary>
	/// Asynchronously inserts a new user.
	/// </summary>
	/// <param name="dto">The data transfer object containing the user information to insert.</param>
	/// <returns>A task that represents the asynchronous operation. The task result contains the identifier of the newly created user.</returns>
	[ServiceOperation(ServiceOperationVerbs.Post)]
	Task<long> Insert(IInsertUserDto dto);

	/// <summary>
	/// Asynchronously updates an existing user.
	/// </summary>
	/// <param name="dto">The data transfer object containing the updated user information.</param>
	/// <returns>A task that represents the asynchronous operation.</returns>
	[ServiceOperation(ServiceOperationVerbs.Put)]
	Task Update(IUpdateUserDto dto);

	/// <summary>
	/// Asynchronously updates a user's password.
	/// </summary>
	/// <param name="dto">The data transfer object containing the password update information.</param>
	/// <returns>A task that represents the asynchronous operation.</returns>
	[ServiceOperation(ServiceOperationVerbs.Put), ServiceUrl(IdentitiesUrls.UpdatePasswordOperation)]
	Task Update(IUpdatePasswordDto dto);

	/// <summary>
	/// Asynchronously deletes a user by its primary key.
	/// </summary>
	/// <param name="dto">The data transfer object containing the primary key of the user to delete.</param>
	/// <returns>A task that represents the asynchronous operation.</returns>
	[ServiceOperation(ServiceOperationVerbs.Delete)]
	Task Delete(IPrimaryKeyDto<long> dto);
}