namespace Connected.Storage.Containers;

/// <summary>
/// Creates and drops storage containers for one storage connection type.
/// </summary>
/// <remarks>
/// Each storage provider implements this middleware for its own connection type. <see cref="IStorageContainerService"/>
/// uses the provider whose <see cref="ConnectionType"/> is the connection type the storage resolves, so a container
/// is always managed by the product that stores the data.
/// </remarks>
public interface IStorageContainerProvider
	: IMiddleware
{
	/// <summary>
	/// Gets the storage connection type this provider manages containers for.
	/// </summary>
	Type ConnectionType { get; }

	/// <summary>
	/// Creates a container beside the default storage, unless it exists.
	/// </summary>
	/// <param name="dto">The DTO naming the container.</param>
	/// <returns>The connection string an application stores its entities in the container with.</returns>
	/// <exception cref="InvalidOperationException">Storage that is not a container exists under the name.</exception>
	Task<string> Create(IStorageContainerDto dto);

	/// <summary>
	/// Drops a container beside the default storage, with everything in it, if it exists.
	/// </summary>
	/// <param name="dto">The DTO naming the container.</param>
	/// <exception cref="InvalidOperationException">The container is the default storage, or storage that is not a container exists under the name.</exception>
	Task Drop(IStorageContainerDto dto);
}
