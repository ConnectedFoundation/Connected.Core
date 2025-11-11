using Connected.Services;
using System.Collections.Immutable;

namespace Connected.Identities.Schemas;

/// <summary>
/// Provides operations for querying and selecting identity descriptors.
/// </summary>
/// <remarks>
/// This interface extends middleware functionality to support identity descriptor retrieval
/// operations. It allows querying all descriptors, filtering by specific values or value lists,
/// and querying dependencies such as users belonging to a specified role. Each provider
/// implementation is identified by a unique name.
/// </remarks>
public interface IIdentityDescriptorProvider
	: IMiddleware
{
	/// <summary>
	/// Gets the unique name identifier for this provider.
	/// </summary>
	string Name { get; }

	/// <summary>
	/// Asynchronously queries all identity descriptors.
	/// </summary>
	/// <returns>A task that represents the asynchronous operation. The task result contains an immutable list of all identity descriptors.</returns>
	Task<IImmutableList<IIdentityDescriptor>> Query();

	/// <summary>
	/// Asynchronously queries identity descriptors by a list of values.
	/// </summary>
	/// <param name="dto">The data transfer object containing the list of values to query.</param>
	/// <returns>A task that represents the asynchronous operation. The task result contains an immutable list of identity descriptors matching the specified values.</returns>
	Task<IImmutableList<IIdentityDescriptor>> Query(IValueListDto<string> dto);

	/// <summary>
	/// Asynchronously queries identity descriptor dependencies by a specific value.
	/// </summary>
	/// <param name="dto">The data transfer object containing the value to query dependencies for.</param>
	/// <returns>A task that represents the asynchronous operation. The task result contains an immutable list of identity descriptors that are dependencies of the specified value, such as users belonging to a specified role.</returns>
	Task<IImmutableList<IIdentityDescriptor>> Query(IValueDto<string> dto);

	/// <summary>
	/// Asynchronously selects a single identity descriptor by a specific value.
	/// </summary>
	/// <param name="dto">The data transfer object containing the value to select.</param>
	/// <returns>A task that represents the asynchronous operation. The task result contains the identity descriptor if found, or null otherwise.</returns>
	Task<IIdentityDescriptor?> Select(IValueDto<string> dto);
}